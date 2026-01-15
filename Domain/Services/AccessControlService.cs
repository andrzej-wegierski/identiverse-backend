using Domain.Abstractions;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<AccessControlService> _logger;

    public AccessControlService(
        ICurrentUserContext user, 
        IIdentityProfileRepository profiles,
        IIdentityService identity,
        ILogger<AccessControlService> logger)
    {
        _user = user;
        _profiles = profiles;
        _identity = identity;
        _logger = logger;
    }

    public async Task CanAccessPersonAsync(int personId, CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated)
        {
            _logger.LogWarning("Access denied: User is not authenticated. Target PersonId: {PersonId}", personId);
            throw new UnauthorizedIdentiverseException("User is not authenticated");
        }

        if (_user.IsAdmin)
        {
            _logger.LogInformation("Admin access granted for User {UserId} to Person {PersonId}", _user.UserId, personId);
            return;
        }
        
        var userId = await _identity.GetUserIdByPersonIdAsync(personId, ct);
        if (!userId.HasValue)
        {
            _logger.LogWarning("Access denied: Person {PersonId} not found. Requested by User {UserId}", personId, _user.UserId);
        }

        if (!userId.HasValue || userId.Value != _user.UserId)
        {
            _logger.LogWarning("Access denied: Ownership mismatch. User {UserId} tried to access Person {PersonId}. Owner: {OwnerId}", 
                _user.UserId, personId, userId);
            throw new ForbiddenException("User has no access to this person");
        }
        
        _logger.LogInformation("Access granted: User {UserId} accessed Person {PersonId}", _user.UserId, personId);
    }

    public async Task EnsureCanAccessProfileAsync(int profileId, CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated)
        {
            _logger.LogWarning("Access denied: User is not authenticated. Target ProfileId: {ProfileId}", profileId);
            throw new UnauthorizedIdentiverseException("User is not authenticated");
        }
        
        if (_user.IsAdmin)
        {
            _logger.LogInformation("Admin access granted for User {UserId} to Profile {ProfileId}", _user.UserId, profileId);
            return;
        }
        
        var personId = await _profiles.GetPersonIdByProfileIdAsync(profileId, ct);
        if (!personId.HasValue)
        {
            _logger.LogWarning("Access denied: Profile {ProfileId} not found. Requested by User {UserId}", profileId, _user.UserId);
            throw new NotFoundException("Profile not found");
        }
        
        var userId = await _identity.GetUserIdByPersonIdAsync(personId.Value, ct);
        if (!userId.HasValue || userId.Value != _user.UserId)
        {
            _logger.LogWarning("Access denied: Ownership mismatch. User {UserId} tried to access Profile {ProfileId} (Person {PersonId}). Owner: {OwnerId}", 
                _user.UserId, profileId, personId, userId);
            throw new ForbiddenException("User has no access to this identity profile");
        }

        _logger.LogInformation("Access granted: User {UserId} accessed Profile {ProfileId}", _user.UserId, profileId);
    }
}