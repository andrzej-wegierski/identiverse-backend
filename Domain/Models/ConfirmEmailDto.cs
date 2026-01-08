using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class ConfirmEmailDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
    
    [Required]
    public required string Token { get; init; }
}