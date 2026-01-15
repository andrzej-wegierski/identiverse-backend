using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Domain.Services;

public interface IIdentityProfileService
{
    Task<List<IdentityProfileDto>> GetProfilesByPersonAsync(int personId, CancellationToken ct = default);
    Task<IdentityProfileDto?> GetProfileByIdForPersonAsync(int personId, int identityId, CancellationToken ct = default);
    Task<IdentityProfileDto> CreateProfileAsync(int personId, CreateIdentityProfileDto dto, CancellationToken ct = default);
    Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default);
    Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default);
    Task<bool> SetDefaultProfileAsync(int personId, int profileId, CancellationToken ct = default);
    Task<bool> UnsetDefaultProfileAsync(int personId, int profileId, CancellationToken ct = default);
    
    Task<IdentityProfileDto?> GetPreferredProfileAsync(int personId, IdentityContext context, CancellationToken ct = default);
}

public class IdentityProfileService : IIdentityProfileService
{
    private readonly IIdentityProfileRepository _repo;
    private readonly IAccessControlService _access;
    private readonly ILogger<IdentityProfileService> _logger;

    public IdentityProfileService(IIdentityProfileRepository repo, IAccessControlService access, ILogger<IdentityProfileService> logger)
    {
        _repo = repo;
        _access = access;
        _logger = logger;
    }


    public async Task<List<IdentityProfileDto>> GetProfilesByPersonAsync(int personId, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);
        return await _repo.GetProfilesByPersonAsync(personId, ct);
    }
    
    public async Task<IdentityProfileDto?> GetProfileByIdForPersonAsync(int personId, int identityId, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);
        var profile = await _repo.GetProfileByIdAsync(identityId, ct);
        if (profile is null || profile.PersonId != personId)
            return null;

        return profile;
    }

    public  async Task<IdentityProfileDto> CreateProfileAsync(int personId, CreateIdentityProfileDto dto,
        CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);
        var created = await _repo.CreateProfileAsync(personId, dto, ct);
        _logger.LogInformation("Profile {ProfileId} created for Person {PersonId}", created.Id, personId);
        return created;
    }

    public async Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default)
    {
        await _access.EnsureCanAccessProfileAsync(id, ct);
        var updated = await _repo.UpdateProfileAsync(id, dto, ct);
        if (updated != null)
        {
            _logger.LogInformation("Profile {ProfileId} updated", id);
        }
        return updated;
    }

    public async Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default)
    {
        await _access.EnsureCanAccessProfileAsync(id, ct);
        var deleted = await _repo.DeleteProfileAsync(id, ct);
        if (deleted)
        {
            _logger.LogInformation("Profile {ProfileId} deleted", id);
        }
        return deleted;
    }

    public async Task<bool> SetDefaultProfileAsync(int personId, int profileId, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);
        
        var profile = await _repo.GetProfileByIdAsync(profileId, ct);
        if (profile is null || profile.PersonId != personId)
        {
            _logger.LogWarning("Failed to set default profile: Profile {ProfileId} not found or doesn't belong to Person {PersonId}", profileId, personId);
            return false;
        }
        
        var result = await _repo.SetAsDefaultAsync(profileId, ct);
        if (result)
        {
            _logger.LogInformation("Profile {ProfileId} set as default for Person {PersonId}", profileId, personId);
        }
        return result;
    }

    public async Task<bool> UnsetDefaultProfileAsync(int personId, int profileId, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);

        var profile = await _repo.GetProfileByIdAsync(profileId, ct);
        if (profile is null || profile.PersonId != personId)
        {
            _logger.LogWarning("Failed to unset default profile: Profile {ProfileId} not found or doesn't belong to Person {PersonId}", profileId, personId);
            return false;
        }

        var result = await _repo.UnsetDefaultAsync(profileId, ct);
        if (result)
        {
            _logger.LogInformation("Profile {ProfileId} unset as default for Person {PersonId}", profileId, personId);
        }
        return result;
    }

    public async Task<IdentityProfileDto?> GetPreferredProfileAsync(
        int personId, 
        IdentityContext context, 
        CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);
        
        var profiles = await _repo.GetProfilesByPersonAsync(personId, ct);
        
        var matchingProfiles = profiles.Where(p => p.Context == context).ToList();
        if (matchingProfiles.Count == 0)
        {
            _logger.LogInformation("No matching profiles found for Person {PersonId} in context {Context}", personId, context);
            return null;
        }

        var defaultProfiles = matchingProfiles.Where(p => p.IsDefaultForContext).ToList();
        if (defaultProfiles.Count > 0)
        {
            var preferred = defaultProfiles.OrderBy(p => p.Id).First();
            _logger.LogInformation("Default profile {ProfileId} selected for Person {PersonId} in context {Context}", preferred.Id, personId, context);
            return preferred;
        }

        var fallback = matchingProfiles
            .OrderBy(p => p.DisplayName)
            .ThenBy(p => p.Id)
            .First();
        
        _logger.LogInformation("Fallback profile {ProfileId} selected for Person {PersonId} in context {Context} (no default found)", fallback.Id, personId, context);
        return fallback;
    }
}