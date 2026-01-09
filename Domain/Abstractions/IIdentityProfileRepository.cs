using Domain.Models;

namespace Domain.Abstractions;

public interface IIdentityProfileRepository
{
    Task<List<IdentityProfileDto>> GetProfilesByPersonAsync(int personId, CancellationToken ct = default);
    Task<IdentityProfileDto?> GetProfileByIdAsync(int id, CancellationToken ct = default);
    Task<IdentityProfileDto> CreateProfileAsync(int personId, CreateIdentityProfileDto dto, CancellationToken ct = default);
    Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default);
    Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default);
    Task<int?> GetPersonIdByProfileIdAsync(int profileId, CancellationToken ct = default);
    Task SetAsDefaultAsync(int profileId, CancellationToken ct = default);
}