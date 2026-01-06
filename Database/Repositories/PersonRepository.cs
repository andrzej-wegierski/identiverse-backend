using Database.Factories;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly IdentiverseDbContext _db;
    private readonly IPersonFactory _factory;

    public PersonRepository(IdentiverseDbContext db, IPersonFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    public async Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default)
    {
        var entities = await _db.Persons.AsNoTracking().ToListAsync(ct);
        return entities.Select(_factory.ToDto).ToList();
    }

    public async Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Persons.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null : _factory.ToDto(entity);
    }

    public async Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default)
    {
        var entity = _factory.FromCreateDto(dto);
        
        await _db.Persons.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return _factory.ToDto(entity);
    }

    public async Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default)
    {
        var entity = await _db.Persons.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return null;
        
        _factory.UpdateEntityFromDto(entity, person);
        await _db.SaveChangesAsync(ct);
        return _factory.ToDto(entity);
    }

    public async Task<bool> DeletePersonAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Persons.SingleOrDefaultAsync(p => p.Id == id, cancellationToken: ct);
        if (entity is null)
            return false;
            
        _db.Persons.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}