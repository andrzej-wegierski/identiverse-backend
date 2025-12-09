using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models;

public class IdentityProfileDto
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public IdentityContext Context { get; init; }
    public string? Language { get; init; }
    public bool IsDefaultForContext { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class CreateIdentityProfileDto
{
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;
    public IdentityContext Context { get; init; } 
    [MaxLength(10)]
    public string? Language { get; init; }
    public bool IsDefaultForContext { get; init; } = false;
}

public class UpdateIdentityProfileDto
{
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;
    public IdentityContext Context { get; init; } 
    [MaxLength(10)]
    public string? Language { get; init; }
    public bool IsDefaultForContext { get; init; } 
}