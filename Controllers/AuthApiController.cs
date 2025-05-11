using API_ASP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthApiController : ControllerBase
{
    private readonly ASPBDContext _context;
    private readonly IConfiguration _configuration;

    public AuthApiController(ASPBDContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

   
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest registerRequest)
    {
        if (string.IsNullOrEmpty(registerRequest.Login))
            return BadRequest(new { Message = "Login is required" });

        if (string.IsNullOrEmpty(registerRequest.Password))
            return BadRequest(new { Message = "Password is required" });

        if (_context.Users.Any(u => u.Login == registerRequest.Login))
            return BadRequest(new { Message = "User with this login already exists" });

        var passwordHash = HashPassword(registerRequest.Password);

        var newUser = new User
        {
            Login = registerRequest.Login,
            Email = registerRequest.Email,
            Password = passwordHash,
            RoleId = 2
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        var token = GenerateJwtToken(newUser,"1");
        return Ok(new { Token = token });
    }

    [HttpGet("profile")]
    [Authorize]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var intUserId))
            return Unauthorized(new { Message = "Invalid user ID" });

        var user = _context.Users.FirstOrDefault(u => u.UserId == intUserId);
        if (user == null)
            return NotFound(new { Message = "User not found." });

        return Ok(user);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
    }

    private bool VerifyPassword(string inputPassword, string storedHash)
    {
        var inputHash = HashPassword(inputPassword);
        return inputHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        var user = _context.Users
            .FirstOrDefault(u => u.Login.ToLower() == loginRequest.Login.ToLower());

        if (user == null || !VerifyPassword(loginRequest.Password, user.Password))
        {
            return Unauthorized(new { Message = "Неверные учетные данные." });
        }

        string userRole = user.RoleId switch
        {
            2 => "admin", 
            1 => "user",  
            _ => "user"   
        };

        var token = GenerateJwtToken(user, userRole);

        return Ok(new
        {
            Token = token,
            User = new
            {
                user.UserId,
                user.Login,
                user.Email,
                Role = userRole  
            }
        });
    }

    private string GenerateJwtToken(User user, string userRole)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Login),
        new Claim(ClaimTypes.Role, userRole) 
    };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [HttpGet("user-by-login/{login}")]
    [Authorize]
    public IActionResult GetUserByLogin(string login)
    {
        var user = _context.Users
            .FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

        if (user == null)
            return NotFound(new { Message = "Пользователь не найден" });

        return Ok(new
        {
            user.UserId,
            user.Login,
            user.Email,
            user.RoleId
        });
    }
}

public class LoginRequest
{
    public string Login { get; set; }
    public string Password { get; set; }
}

public class RegisterRequest
{
    public string Login { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}