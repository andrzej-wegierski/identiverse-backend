using Database.Factories;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repositories;

public class IdentityProfileRepository : IIdentityProfileRepository
{
    private readonly IdentiverseDbContext _db;
    private readonly IIdentityProfileFactory _factory;


    public IdentityProfileRepository(IdentiverseDbContext db, IIdentityProfileFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    public async Task<List<IdentityProfileDto>> GetProfilesByPersonAsync(int personId, CancellationToken ct = default)
    {
        var query = _db.IdentityProfiles
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .OrderBy(p => p.Context)
            .ThenByDescending(p => p.IsDefaultForContext)
            .ThenBy(p => p.DisplayName);
        
        var list = await query.ToListAsync(ct);
        return list.Select(_factory.ToDto).ToList();
    }

    public async Task<IdentityProfileDto?> GetProfileByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.IdentityProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null : _factory.ToDto(entity);
    }

    public async Task<IdentityProfileDto> CreateProfileAsync(int personId, CreateIdentityProfileDto dto, CancellationToken ct = default)
    {
        var entity = _factory.FromCreate(personId, dto);
        _db.IdentityProfiles.Add(entity);
        await _db.SaveChangesAsync(ct);
        return _factory.ToDto(entity);
    }

    public async Task<IdentityProfileDto?> UpdateProfileAsync(int id, UpdateIdentityProfileDto dto, CancellationToken ct = default)
    {
        var entity = await _db.IdentityProfiles.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return null;
        
        _factory.UpdateEntity(entity, dto);
        await _db.SaveChangesAsync(ct);
        return _factory.ToDto(entity);
    }

    public async Task<bool> DeleteProfileAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.IdentityProfiles.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return false;
        
        _db.IdentityProfiles.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int?> GetPersonIdByProfileIdAsync(int profileId, CancellationToken ct = default)
    {
        return await _db.IdentityProfiles
            .Where(p => p.Id == profileId)
            .Select(p => (int?)p.PersonId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> SetAsDefaultAsync(int profileId, CancellationToken ct = default)
    {
        var target = await _db.IdentityProfiles.FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (target is null)
            return false;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var existingDefault = await _db.IdentityProfiles
                .Where(p => p.PersonId == target.PersonId && p.Context == target.Context && p.IsDefaultForContext)
                .FirstOrDefaultAsync(ct);

            if (existingDefault is not null)
            {
                existingDefault.IsDefaultForContext = false;
                existingDefault.UpdatedAt = DateTime.UtcNow;
            }

            target.IsDefaultForContext = true;
            target.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}