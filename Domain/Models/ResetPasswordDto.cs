using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; } 
    
    [Required]
    public required string Token { get; init; }
    
    [Required]
    [MinLength(10, ErrorMessage = "Password must be at least 10 characters long.")]
    [RegularExpression(
        pattern: "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\w\\s]).{10,}$",
        ErrorMessage = "Password must have at least one uppercase, one lowercase, one digit, and one special character.")]    
    public required string NewPassword { get; init; } = string.Empty;
    
}