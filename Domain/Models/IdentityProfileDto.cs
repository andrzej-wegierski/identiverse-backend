using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models;

public class IdentityProfileDto
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public IdentityContext Context { get; init; }
    
    public DateTime? BirthDate { get; init; }
    [MaxLength(50)]
    public string? Title { get; init; }
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }
    [Phone]
    [MaxLength(50)]
    public string? Phone { get; init; }
    [MaxLength(500)]
    public string? Address { get; init; }
    
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
    
    public DateTime? BirthDate { get; init; }
    [MaxLength(50)]
    public string? Title { get; init; }
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }
    [Phone]
    [MaxLength(50)]
    public string? Phone { get; init; }
    [MaxLength(500)]
    public string? Address { get; init; }
}

public class UpdateIdentityProfileDto
{
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;
    public IdentityContext Context { get; init; } 
    
    public DateTime? BirthDate { get; init; }
    [MaxLength(50)]
    public string? Title { get; init; }
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }
    [Phone]
    [MaxLength(50)]
    public string? Phone { get; init; }
    [MaxLength(500)]
    public string? Address { get; init; }
}