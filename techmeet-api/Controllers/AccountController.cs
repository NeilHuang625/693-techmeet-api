using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using techmeet_api.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using techmeet_api.Data;
using Microsoft.AspNetCore.Authorization;

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

        [HttpPost("verify")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            var user = HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                return Ok(new { valid = true });
            }
            else
            {
                return Unauthorized(new { valid = false });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Convert the model to a JSON string
            var modelJson = System.Text.Json.JsonSerializer.Serialize(model);

            // Print the JSON string
            Console.WriteLine(modelJson);
            
            // Check if the email has been used
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if(userExists != null){
                return BadRequest("Email already in use");
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Nickname = model.Nickname
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Add user to the "user" role
                await _userManager.AddToRoleAsync(user, "user");
                await _signInManager.SignInAsync(user, false);
                return Ok(new { Token = GenerateJwtToken(model.Email, user) });
            }

            return BadRequest("Invalid registration");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return BadRequest("Invalid login attempt");
                }
                return Ok(new { Token = GenerateJwtToken(model.Email, user) });
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
            // Debug: Check if the user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                Console.WriteLine($"User is authenticated: {User.Identity.Name}");
            }
            else
            {
                Console.WriteLine("User is not authenticated");
            }
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

        private bool IsJwtRevoked(string jwt)
        {
            return _context.RevokedTokens.Any(rt => rt.Token == jwt);
        }

        private async Task<string> GenerateJwtToken(string email, User user)
        {
            var userClaims = new List<Claim>{
                new Claim(ClaimTypes.Name, email)
            };

            if (user != null)
            {
                userClaims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            }
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                userClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt_Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_configuration["Jwt_Issuer"],
                _configuration["Jwt_Issuer"],
                userClaims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}