using Database.Entities;
using Domain.Abstractions;
using Domain.Models;

namespace Database.Factories;

public interface IPersonFactory
{
    PersonDto ToDto(Person entity);
    Person FromDto(PersonDto dto);
    Person FromCreateDto(CreatePersonDto dto);
    void UpdateEntityFromDto(Person entity, UpdatePersonDto dto);
}

public class PersonFactory : IPersonFactory 
{
    public PersonDto ToDto(Person entity) => new()
    {
        Id = entity.Id,
        ExternalId = entity.ExternalId,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        PreferredName = entity.PreferredName,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public Person FromDto(PersonDto dto) => new()
    {
        Id = dto.Id,
        ExternalId = dto.ExternalId,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        PreferredName = dto.PreferredName,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };

    public Person FromCreateDto(CreatePersonDto dto) => new()
    {
        ExternalId = Guid.NewGuid(),
        FirstName = dto.FirstName.Trim(),
        LastName = dto.LastName.Trim(),
        PreferredName = dto.PreferredName?.Trim(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void UpdateEntityFromDto(Person entity, UpdatePersonDto dto)
    {
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.PreferredName = dto.PreferredName;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}