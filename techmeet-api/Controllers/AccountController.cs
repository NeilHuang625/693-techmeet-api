using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using techmeet_api.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using techmeet_api.Data;

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
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
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
                return Ok(new { Token = GenerateJwtToken(model.Email) });
            }

            return BadRequest("Invalid login attempt");
        }

        private string GetJwtFromRequest()
        {
            var jwt = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            return jwt;
        }

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

        private string GenerateJwtToken(string email, User user = null)
        {
            var userClaims = new List<Claim>{
                new Claim(ClaimTypes.Name, email)
            };

            if (user != null)
            {
                userClaims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                userClaims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}