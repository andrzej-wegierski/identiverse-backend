using Domain.Abstractions;
using Domain.Models;

namespace Domain.Services;

public interface IPersonService
{
    Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default);
    Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default);
    Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default);
    Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default);
    Task<bool> DeletePersonAsync(int id, CancellationToken ct = default);
}

public class PersonService : IPersonService
{
    private readonly IPersonRepository _repo;

    public PersonService(IPersonRepository repo)
    {
        _repo = repo;
    }


    public Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default)
        => _repo.GetPersonsAsync(ct);

    public Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default)
        =>  _repo.GetPersonByIdAsync(id,  ct);

    public Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default)
        => _repo.CreatePersonAsync(dto, ct);
    
    public Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default)
        => _repo.UpdatePersonAsync(id, person, ct);

    public Task<bool> DeletePersonAsync(int id, CancellationToken ct = default)
        => _repo.DeletePersonAsync(id, ct);
    
}