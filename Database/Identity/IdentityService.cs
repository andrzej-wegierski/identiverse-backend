using Database.Entities;
using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Domain.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            PersonId = user.PersonId,
            Role = Enum.TryParse<UserRole>(roles.FirstOrDefault() ?? "User", out var role) ? role : UserRole.User
        };
    }

    public async Task<bool> LinkPersonToUserAsync(int userId, int personId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;
        
        user.PersonId = personId;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<int?> GetuserIdByPersonIdAsync(int personId, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .Where(u => u.PersonId == personId)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(ct);
        
        return user?.Id;
    }
}