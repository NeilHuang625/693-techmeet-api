using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using techmeet_api.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using techmeet_api.Data;
using Microsoft.AspNetCore.Authorization;
using Azure.Storage.Blobs;


namespace techmeet_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(IFormFile imageFile)
        {
            var model = new RegisterModel
            {
                Email = Request.Form["email"].ToString(),
                Password = Request.Form["password"].ToString(),
                Nickname = Request.Form["nickname"].ToString()
            };
            // Check if the email has been used
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return BadRequest("Email already in use");
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Nickname = model.Nickname
            };

            // Create a blob service client to interact with the Azure blob storage
            var blobServiceClient = new BlobServiceClient(_configuration["BLOB_STORAGE_CONNECTION_STRING"]);
            var containerClient = blobServiceClient.GetBlobContainerClient("uploads"); // Create a container called "uploads"
            await containerClient.CreateIfNotExistsAsync(); // Create the container if it doesn't exist
            // Generate a unique file name for the image
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            // Create a blob client for the image file
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Upload the image to the Azure blob storage
            using (var stream = imageFile.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            user.ProfilePhotoUrl = blobClient.Uri.AbsoluteUri;

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Add user to the "user" role
                await _userManager.AddToRoleAsync(user, "user");
                await _signInManager.SignInAsync(user, false);

                return Ok(new { User = new { Id = user.Id, Email = user.Email, Nickname = user.Nickname, Roles = "user", ImgUrl = user.ProfilePhotoUrl }, Token = GenerateJwtToken(model.Email, user) });
            }
            else
            {
                return BadRequest("Invalid registration");
            }

        }

        private bool IsJwtRevoked(string jwt)
        {
            return _context.RevokedTokens.Any(rt => rt.Token == jwt);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model.Email == null || model.Password == null) return BadRequest("Invalid login attempt");
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                IList<string>? roles = null;
                if (user == null)
                {
                    return BadRequest("Invalid login attempt");
                }
                else
                {
                    roles = await _userManager.GetRolesAsync(user);
                }
                return Ok(new { User = new { Id = user.Id, Email = user.Email, Nickname = user.Nickname, Roles = roles, ImgUrl = user.ProfilePhotoUrl }, Token = GenerateJwtToken(model.Email, user) });
            }

            return BadRequest("Invalid login attempt");
        }

        private string GetJwtFromRequest()
        {
            var jwt = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            return jwt;
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Get the JWT from the request
            string jwt = GetJwtFromRequest();

            // Create a new RevokedToken object
            var revokedToken = new RevokedToken
            {
                Token = jwt,
                RevokedAt = DateTime.UtcNow
            };

            // Add the RevokedToken to the database
            _context.RevokedTokens.Add(revokedToken);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            string jwt = GetJwtFromRequest();
            if (IsJwtRevoked(jwt))
            {
                return BadRequest("Invalid Token");
            }
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var email = token.Claims.First(claim => claim.Type == ClaimTypes.Name).Value;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest("Invalid user");
            }

            // Revoke the old jwt token
            var revokedToken = new RevokedToken
            {
                Token = jwt,
                RevokedAt = DateTime.UtcNow
            };
            _context.RevokedTokens.Add(revokedToken);
            await _context.SaveChangesAsync();

            return Ok(new { Token = GenerateJwtToken(email, user) });
        }

        [Authorize(Roles = "user")]
        [HttpPost("upgrade-to-vip")]
        public async Task<IActionResult> UpgradeToVip()
        {
            var jwt = GetJwtFromRequest();
            if (IsJwtRevoked(jwt))
            {
                return BadRequest("Invalid Token");
            }
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var email = token.Claims.First(claim => claim.Type == ClaimTypes.Name).Value;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest("Invalid user");
            }
            await _userManager.RemoveFromRoleAsync(user, "user");
            await _userManager.AddToRoleAsync(user, "vip");

            // Update the VIPExpirationDate field
            user.VIPExpirationDate = DateTime.UtcNow.AddYears(1);
            await _userManager.UpdateAsync(user);

            var revokedToken = new RevokedToken
            {
                Token = jwt,
                RevokedAt = DateTime.UtcNow
            };
            _context.RevokedTokens.Add(revokedToken);
            await _context.SaveChangesAsync();

            return Ok(new { Token = GenerateJwtToken(email, user) });
        }


        private async Task<string> GenerateJwtToken(string email, User user)
        {
            var userClaims = new List<Claim>{
                new Claim(ClaimTypes.Name, email)
            };

            if (user != null)
            {
                userClaims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
                // Add the user's nickname to the claims
                userClaims.Add(new Claim(ClaimTypes.GivenName, user.Nickname ?? string.Empty));
                userClaims.Add(new Claim("ProfilePhotoUrl", user.ProfilePhotoUrl ?? string.Empty));
            }
            if (user == null)
            {
                throw new Exception("User is null");
            }
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                userClaims.Add(new Claim(ClaimTypes.Role, role));
            }
            var jwtKey = _configuration["Jwt_Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("JWT configuration is missing");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_configuration["Jwt_Issuer"],
                _configuration["Jwt_Audience"],
                userClaims,
                expires: DateTime.UtcNow.AddSeconds(15), // Token expires in 6 hours
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserInfo(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(new { Id = user.Id, Email = user.Email, Nickname = user.Nickname, Roles = await _userManager.GetRolesAsync(user), ImgUrl = user.ProfilePhotoUrl });
        }
    }
}