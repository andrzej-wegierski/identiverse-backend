namespace Domain.Models;

public class AdminUserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int? PersonId { get; init; }
    public List<string> Roles { get; init; } = [];
}