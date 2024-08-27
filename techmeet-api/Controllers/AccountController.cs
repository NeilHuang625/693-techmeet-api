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

        [Authorize]
        [HttpPost("userinfo")]
        public async Task<IActionResult> UserInfo(){
            string jwt = GetJwtFromRequest();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var email = token.Claims.First(claim => claim.Type == ClaimTypes.Name).Value;
            var user = await _userManager.FindByEmailAsync(email);
            IList<string> roles = null;
            if (user == null)
            {
                return BadRequest("Invalid user");
            }else{
                roles = await _userManager.GetRolesAsync(user);
            }
            return Ok(new {User = new{Id = user.Id, Email = user.Email, Nickname = user.Nickname, Roles = roles}});
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
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

                return Ok(new {User = new{Id = user.Id, Email = user.Email, Nickname = user.Nickname, Roles="user"}, Token = GenerateJwtToken(model.Email, user) });
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
                IList<string> roles = null;
                if (user == null)
                {
                    return BadRequest("Invalid login attempt");
                }else{
                    roles = await _userManager.GetRolesAsync(user);
                }
                return Ok(new {User = new{Id = user.Id, Email = user.Email, Nickname = user.Nickname, Roles = roles}, Token = GenerateJwtToken(model.Email, user) });
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
            var jwtKey = _configuration["Jwt_Key"];
            if(string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("JWT configuration is missing");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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