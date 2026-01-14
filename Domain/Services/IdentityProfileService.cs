using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;

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

    public IdentityProfileService(IIdentityProfileRepository repo, IAccessControlService access)
    {
        _repo = repo;
        _access = access;
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
        return await _repo.CreateProfileAsync(personId, dto, ct);
    }

    public async Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default)
    {
        await _access.EnsureCanAccessProfileAsync(id, ct);
        return await _repo.UpdateProfileAsync(id, dto, ct);
    }

    public async Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default)
    {
        await _access.EnsureCanAccessProfileAsync(id, ct);
        return await _repo.DeleteProfileAsync(id, ct);
    }

    public async Task<bool> SetDefaultProfileAsync(int personId, int profileId, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);
        
        var profile = await _repo.GetProfileByIdAsync(profileId, ct);
        if (profile is null || profile.PersonId != personId)
            return false;
        
        return await _repo.SetAsDefaultAsync(profileId, ct);
    }

    public async Task<bool> UnsetDefaultProfileAsync(int personId, int profileId, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);

        var profile = await _repo.GetProfileByIdAsync(profileId, ct);
        if (profile is null || profile.PersonId != personId)
            return false;

        return await _repo.UnsetDefaultAsync(profileId, ct);
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
            return null;

        var defaultProfiles = matchingProfiles.Where(p => p.IsDefaultForContext).ToList();
        if (defaultProfiles.Count > 0)
        {
            return defaultProfiles.OrderBy(p => p.Id).First();
        }

        return matchingProfiles
            .OrderBy(p => p.DisplayName)
            .ThenBy(p => p.Id)
            .First();
    }
}