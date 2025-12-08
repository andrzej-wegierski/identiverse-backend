namespace Domain.Models;

public class AuthUserData
{
    public UserDto User { get; init; } = new();
    public string PasswordHash { get; init; } = string.Empty;
    public string PasswordSalt { get; init; } = string.Empty;
}