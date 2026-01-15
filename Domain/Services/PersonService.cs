using Domain.Abstractions;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Domain.Services;

public interface IPersonService
{
    Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default);
    Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default);
    Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default);
    Task<PersonDto> CreatePersonForCurrentUserAsync(CreatePersonDto dto, CancellationToken ct = default);
    Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default);
    Task<bool> DeletePersonAsync(int id, CancellationToken ct = default);
}

public class PersonService : IPersonService
{
    private readonly IPersonRepository _repo;
    private readonly IIdentityService _identityService;
    private readonly IAccessControlService _access;
    private readonly ICurrentUserContext _current;
    private readonly ILogger<PersonService> _logger;

    public PersonService(
        IPersonRepository repo, 
        IIdentityService identityService,
        IAccessControlService access, 
        ICurrentUserContext current,
        ILogger<PersonService> logger)
    {
        _repo = repo;
        _identityService = identityService;
        _access = access;
        _current = current;
        _logger = logger;
    }

    public Task<List<PersonDto>> GetPersonsAsync(CancellationToken ct = default)
        => _repo.GetPersonsAsync(ct);

    public async Task<PersonDto?> GetPersonByIdAsync(int id, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(id, ct);
        return await _repo.GetPersonByIdAsync(id, ct);
    }

    public async Task<PersonDto> CreatePersonAsync(CreatePersonDto dto, CancellationToken ct = default)
    {
        var person = await _repo.CreatePersonAsync(dto, ct);
        _logger.LogInformation("Person {PersonId} created", person.Id);
        return person;
    }

    public async Task<PersonDto> CreatePersonForCurrentUserAsync(CreatePersonDto dto, CancellationToken ct = default)
    {
        var user = await _identityService.GetUserByIdAsync(_current.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning("Attempted to create person for non-existent user {UserId}", _current.UserId);
            throw new NotFoundException("User not found");
        }
        
        if (user.PersonId.HasValue)
        {
            _logger.LogWarning("User {UserId} already has a person {PersonId}", _current.UserId, user.PersonId.Value);
            throw new ConflictException("Person already exists for this user. Update instead.");
        }
        
        var created = await _repo.CreatePersonAsync(dto, ct);
        _logger.LogInformation("Person {PersonId} created for User {UserId}", created.Id, user.Id);

        var linked = await _identityService.LinkPersonToUserAsync(user.Id, created.Id, ct);
        if (!linked)
        {
            _logger.LogError("Failed to link User {UserId} to created Person {PersonId}", user.Id, created.Id);
            throw new InvalidOperationException("Failed to link user to created person");
        }
        
        _logger.LogInformation("Person {PersonId} successfully linked to User {UserId}", created.Id, user.Id);
        return created;
    }

    public async Task<PersonDto?> UpdatePersonAsync(int id, UpdatePersonDto person, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(id, ct);
        var updated = await _repo.UpdatePersonAsync(id, person, ct);
        if (updated != null)
        {
            _logger.LogInformation("Person {PersonId} updated", id);
        }
        return updated;
    }

    public async Task<bool> DeletePersonAsync(int id, CancellationToken ct = default)
    {
        await _access.CanAccessPersonAsync(id, ct);
        var deleted = await _repo.DeletePersonAsync(id, ct);
        if (deleted)
        {
            _logger.LogInformation("Person {PersonId} deleted", id);
        }
        return deleted;
    }
}