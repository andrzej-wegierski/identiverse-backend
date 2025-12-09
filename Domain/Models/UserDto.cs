using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class UserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "User";
    public int? PersonId { get; init; }
}

public class RegisterUserDto
{
    [Required]
    [MinLength(8, ErrorMessage = "Username must be at least 8 characters long.")]
    public string Username { get; init; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Required]
    [MinLength(10, ErrorMessage = "Password must be at least 10 characters long.")]
    [RegularExpression(
        pattern: "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\w\\s]).{10,}$",
        ErrorMessage = "Password must have at least one uppercase, one lowercase, one digit, and one special character.")]
    
    public string Password { get; set; } = string.Empty;

    public int? PersonId { get; init; }
}

public class LoginUserDto
{
    public string UsernameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}