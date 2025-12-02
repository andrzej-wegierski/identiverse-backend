using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class UpdatePersonDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;
    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;
    [MaxLength(100)]
    public string? PreferredName { get; init; }
}