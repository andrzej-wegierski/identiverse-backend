using Domain.Abstractions;
using Domain.Models;

namespace Domain.Services;

public interface IIdentityProfileService
{
    Task<List<IdentityProfileDto>> GetProfilesByPersonAsync(int personId, CancellationToken ct = default);
    Task<IdentityProfileDto?> GetProfileByIdAsync(int id, CancellationToken ct = default);
    Task<IdentityProfileDto> CreateProfileAsync(int personId, CreateIdentityProfileDto dto, CancellationToken ct = default);
    Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default);
    Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default);
    
    Task<IdentityProfileDto?> GetPreferredProfileAsync(int personId, string context, string? language, CancellationToken ct = default);
}

public class IdentityProfileService : IIdentityProfileService
{
    private readonly IIdentityProfileRepository _repo;

    public IdentityProfileService(IIdentityProfileRepository repo)
    {
        _repo = repo;
    }


    public Task<List<IdentityProfileDto>> GetProfilesByPersonAsync(int personId, CancellationToken ct = default)
        => _repo.GetProfilesByPersonAsync(personId, ct);

    public Task<IdentityProfileDto?> GetProfileByIdAsync(int id, CancellationToken ct = default)
        => _repo.GetProfileByIdAsync(id, ct);

    public async Task<IdentityProfileDto> CreateProfileAsync(int personId, CreateIdentityProfileDto dto,
        CancellationToken ct = default)
        => await _repo.CreateProfileAsync(personId, dto, ct);

    public Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default)
        => _repo.UpdateProfileAsync(id, dto, ct);

    public Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default)
        => _repo.DeleteProfileAsync(id, ct);

    public async Task<IdentityProfileDto?> GetPreferredProfileAsync(int personId, string context, string? language = null, CancellationToken ct = default)
    {
        var profiles = await _repo.GetProfilesByPersonAsync(personId, ct);
        var sameContext =
            profiles.Where(p => string.Equals(p.Context, context, System.StringComparison.OrdinalIgnoreCase));

        IEnumerable<IdentityProfileDto> identityProfileDtos = sameContext.ToList();
        if (!string.IsNullOrWhiteSpace(language))
        {
            var langMatch = identityProfileDtos.Where(p =>
                string.Equals(p.Language, language, System.StringComparison.OrdinalIgnoreCase));

            var profileDtos = langMatch.ToList();
            var defLang = profileDtos.FirstOrDefault(p => p.IsDefaultForContext);
            if (defLang != null) 
                return defLang;
            
            var anyLang = profileDtos.FirstOrDefault();
        }

        var def = identityProfileDtos.FirstOrDefault(p => p.IsDefaultForContext);
        if (def != null)
            return def;
        
        return identityProfileDtos.FirstOrDefault();
    }
}