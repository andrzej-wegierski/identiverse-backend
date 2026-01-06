using Domain.Models;

namespace Domain.Abstractions;

public interface IIdentityService
{
    Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<bool> LinkPersonToUserAsync(int userId, int personId, CancellationToken ct = default);
}