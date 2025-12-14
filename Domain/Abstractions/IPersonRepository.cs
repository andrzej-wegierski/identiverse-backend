using Domain.Models;

namespace Domain.Abstractions;

public interface IPersonRepository
{
    Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default);
    Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default);
    Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default);
    Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default);
    Task<bool> DeletePersonAsync(int id, CancellationToken ct = default);
    Task<int?> GetUserIdByPersonIdAsync(int personId, CancellationToken ct = default);
}