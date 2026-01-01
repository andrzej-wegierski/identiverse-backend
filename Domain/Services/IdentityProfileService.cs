using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;

namespace Domain.Services;

public interface IIdentityProfileService
{
    Task<List<IdentityProfileDto>> GetProfilesByPersonAsync(int personId, CancellationToken ct = default);
    Task<IdentityProfileDto?> GetProfileByIdAsync(int id, CancellationToken ct = default);
    Task<IdentityProfileDto> CreateProfileAsync(int personId, CreateIdentityProfileDto dto, CancellationToken ct = default);
    Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default);
    Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default);
    
    Task<IdentityProfileDto?> GetPreferredProfileAsync(int personId, IdentityContext context, string? language, CancellationToken ct = default);
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

    public async Task<IdentityProfileDto?> GetProfileByIdAsync(int id, CancellationToken ct = default)
    {
        await _access.EnsureCanAccessProfileAsync(id, ct);
        return await _repo.GetProfileByIdAsync(id, ct);
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

    public async Task<IdentityProfileDto?> GetPreferredProfileAsync(
        int personId, 
        IdentityContext context, 
        string? language = null, 
        CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(personId, ct);
        var profiles = await _repo.GetProfilesByPersonAsync(personId, ct);

        var sameContext = profiles
                .Where(p => p.Context == context)
                .ToList();
        
        if (!sameContext.Any()) 
            return null;

        if (!string.IsNullOrWhiteSpace(language))
        {
            var langMatches = sameContext
                .Where(p => string.Equals(p.Language, language, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var defLang = langMatches.FirstOrDefault(p => p.IsDefaultForContext);
            if (defLang is not null)
                return defLang;

            var anyLang = langMatches.FirstOrDefault();
            if (anyLang is not null)
                return anyLang;
        }

        var def = sameContext.FirstOrDefault(p => p.IsDefaultForContext);
        if (def is not null)
            return def;

        return sameContext.FirstOrDefault();
    }
}