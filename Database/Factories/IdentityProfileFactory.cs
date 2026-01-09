using Database.Entities;
using Domain.Models;

namespace Database.Factories;

public interface IIdentityProfileFactory
{
    IdentityProfileDto ToDto(IdentityProfile entity);
    IdentityProfile FromCreate(int personId, CreateIdentityProfileDto dto);
    void UpdateEntity(IdentityProfile entity, UpdateIdentityProfileDto dto);
}

public class IdentityProfileFactory : IIdentityProfileFactory
{
    public IdentityProfileDto ToDto(IdentityProfile entity) => new()
    {
        Id = entity.Id,
        PersonId = entity.PersonId,
        DisplayName = entity.DisplayName,
        Context = entity.Context,
        IsDefaultForContext = entity.IsDefaultForContext,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public IdentityProfile FromCreate(int personId, CreateIdentityProfileDto dto) => new()
    {
        PersonId = personId,
        DisplayName = dto.DisplayName.Trim(),
        Context = dto.Context,
        IsDefaultForContext = dto.IsDefaultForContext,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void UpdateEntity(IdentityProfile entity, UpdateIdentityProfileDto dto)
    {
        entity.DisplayName = dto.DisplayName.Trim();
        entity.Context = dto.Context;
        entity.IsDefaultForContext = dto.IsDefaultForContext;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}