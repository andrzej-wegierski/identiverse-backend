using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class ResendConfirmationDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}