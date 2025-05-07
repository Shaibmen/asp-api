using System.ComponentModel.DataAnnotations;

public class CreateUserDto
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Login { get; set; }

    [Required]
    public string Password { get; set; }

    public int RoleId { get; set; } = 2; // Default to User role
}