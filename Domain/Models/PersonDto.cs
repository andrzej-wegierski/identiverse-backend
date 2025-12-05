using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class PersonDto
{
    public int Id { get; init; }
    public Guid ExternalId { get; init; }
    public string FirstName { get; init;  } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PreferredName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class CreatePersonDto
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