using Domain.Models;

namespace Domain.Abstractions;

public interface IUserRepository
{
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UserDto?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    
    Task<int> RegisterUserAsync(RegisterUserDto user, byte[] passwordHash, byte[] passwordSalt, CancellationToken ct = default);
    Task<AuthUserData?> GetAuthByUserNameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default);
    
    Task<bool> IsUsernameTakenAsync(string username, CancellationToken ct = default);
    Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default);
    
}