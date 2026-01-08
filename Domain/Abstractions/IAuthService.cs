using Domain.Models;

namespace Domain.Abstractions;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto user, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
}