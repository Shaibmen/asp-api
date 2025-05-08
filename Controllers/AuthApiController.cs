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

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        var user = _context.Users.FirstOrDefault(u => u.Login == loginRequest.Login); // Ищем по логину, а не по email

        if (user == null || !VerifyPassword(loginRequest.Password, user.Password))
            return Unauthorized("Invalid credentials.");

        var token = GenerateJwtToken(user.Login);
        return Ok(new { token });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest registerRequest)
    {
        // Проверка на обязательные поля
        if (string.IsNullOrEmpty(registerRequest.Login))
            return BadRequest("Login is required");

        if (string.IsNullOrEmpty(registerRequest.Password))
            return BadRequest("Password is required");

        // Проверка существующего пользователя только по логину
        if (_context.Users.Any(u => u.Login == registerRequest.Login))
            return BadRequest("User with this login already exists");

        // Хэширование пароля
        var passwordHash = HashPassword(registerRequest.Password);

        var newUser = new User
        {
            Login = registerRequest.Login,
            Email = registerRequest.Email, // может быть null
            Password = passwordHash,
            RoleId = 2 // Установите роль по умолчанию, если нужно
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        var token = GenerateJwtToken(newUser.Login);
        return Ok(new { token });
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
    }


    [HttpGet("profile")]
    [Authorize]
    public IActionResult GetProfile()
    {
        var userLogin = User.Identity.Name;
        var user = _context.Users.FirstOrDefault(u => u.Login == userLogin);

        if (user == null)
            return NotFound("User not found.");

        return Ok(user);
    }

    private bool VerifyPassword(string inputPassword, string storedHash)
    {
        using var sha256 = SHA256.Create();
        var hashedInput = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputPassword));
        var inputHash = BitConverter.ToString(hashedInput).Replace("-", "");
        return inputHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
    }

    private string GenerateJwtToken(string email)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Получаем пользователя из БД по email/login
        var user = _context.Users.FirstOrDefault(u => u.Login == email); // или по Email, в зависимости от использования
        if (user == null)
            throw new Exception("User not found");

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Login)
    };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"])),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
