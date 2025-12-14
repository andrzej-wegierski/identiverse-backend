using Domain.Abstractions;
using Domain.Models;

namespace Domain.Services;

public interface IPersonService
{
    Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default);
    Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default);
    Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default);
    Task<PersonDto> CreatePersonForUserAsync(int userId, CreatePersonDto dto, CancellationToken ct = default);
    Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default);
    Task<bool> DeletePersonAsync(int id, CancellationToken ct = default);
}

public class PersonService : IPersonService
{
    private readonly IPersonRepository _repo;
    private readonly IUserRepository _users;

    public PersonService(IPersonRepository repo, IUserRepository users)
    {
        _repo = repo;
        _users = users;
    }

    public Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default)
        => _repo.GetPersonsAsync(ct);

    public Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default)
        =>  _repo.GetPersonByIdAsync(id,  ct);

    public Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default)
        => _repo.CreatePersonAsync(dto, ct);

    public async Task<PersonDto> CreatePersonForUserAsync(int userId, CreatePersonDto dto, CancellationToken ct = default)
    {
        var created = await _repo.CreatePersonAsync(dto, ct);
        if (_users is null)
            throw new InvalidOperationException("User repository is not configured for linking");
        var linked = await _users.SetPersonIdAsync(userId, created.Id, ct);
        if (!linked)
            throw new InvalidOperationException("Failed to link user to created person");
        return created;
    }
    
    public Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default)
        => _repo.UpdatePersonAsync(id, person, ct);

    public Task<bool> DeletePersonAsync(int id, CancellationToken ct = default)
        => _repo.DeletePersonAsync(id, ct);
    
}