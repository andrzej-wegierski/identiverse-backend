namespace Domain.Models;

public class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTimeOffset Expires { get; init; }
    public UserDto? User { get; init; }
}