using API_ASP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace API_ASP.Controllers
{
    [ApiController]
    [Route("AuthApi")]
    public class AuthApiController : ControllerBase
    {
        private readonly ASPBDContext _context;
        private readonly IConfiguration _configuration;

        public AuthApiController(ASPBDContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email and password are required");

            var hashedPassword = HashPassword(request.Password);

            var user = _context.Users.FirstOrDefault(u =>
                u.Email == request.Email && u.Password == hashedPassword);

            if (user == null)
                return Unauthorized("Invalid credentials");

            return Ok(new
            {
                Message = "Login successful",
                User = new
                {
                    user.UserId,
                    user.Email,
                    user.Login,
                    user.RoleId
                }
            });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("User already exists");

            var user = new User
            {
                Email = request.Email,
                Login = request.Login,
                Password = HashPassword(request.Password),
                RoleId = 2 // по умолчанию "User"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User registered successfully");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashed = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashed).Replace("-", "").ToLower();
        }
    }
}
