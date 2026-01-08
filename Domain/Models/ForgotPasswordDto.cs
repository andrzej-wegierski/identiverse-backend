using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}   