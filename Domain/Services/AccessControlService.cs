using Domain.Abstractions;
using Domain.Exceptions;

namespace Domain.Services;

public interface IAccessControlService
{
    Task CanAccessPersonAsync(int personId, CancellationToken ct = default);
    Task EnsureCanAccessProfileAsync(int profileId, CancellationToken ct = default);
}

public class AccessControlService : IAccessControlService
{
    
    private readonly ICurrentUserContext _user;
    private readonly IIdentityProfileRepository _profiles;
    private readonly IIdentityService _identity;

    public AccessControlService(
        ICurrentUserContext user, 
        IIdentityProfileRepository profiles,
        IIdentityService identity)
    {
        _user = user;
        _profiles = profiles;
        _identity = identity;
    }

    public async Task CanAccessPersonAsync(int personId, CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated)
            throw new UnauthorizedIdentiverseException("User is not authenticated");

        if (_user.IsAdmin)
            return;
        
        var userId = await _identity.GetUserIdByPersonIdAsync(personId, ct);
        if (!userId.HasValue || userId.Value != _user.UserId)
            throw new ForbiddenException("User has no access to this person");
    }

    public async Task EnsureCanAccessProfileAsync(int profileId, CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated)
            throw new UnauthorizedIdentiverseException("User is not authenticated");
        
        if (_user.IsAdmin)
            return;
        
        var personId = await _profiles.GetPersonIdByProfileIdAsync(profileId, ct);
        if (!personId.HasValue)
            throw new NotFoundException("Profile not found");
        
        var userId = await _identity.GetUserIdByPersonIdAsync(personId.Value, ct);
        if (!userId.HasValue || userId.Value != _user.UserId)
            throw new ForbiddenException("User has no access to this identity profile");
    }
}