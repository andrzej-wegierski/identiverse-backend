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
        BirthDate = entity.BirthDate,
        Title = entity.Title,
        Email = entity.Email,
        Phone = entity.Phone,
        Address = entity.Address,
        IsDefaultForContext = entity.IsDefaultForContext,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public IdentityProfile FromCreate(int personId, CreateIdentityProfileDto dto) => new()
    {
        PersonId = personId,
        DisplayName = dto.DisplayName.Trim(),
        Context = dto.Context,
        BirthDate = dto.BirthDate,
        Title = dto.Title?.Trim(),
        Email = dto.Email?.Trim(),
        Phone = dto.Phone?.Trim(),
        Address = dto.Address?.Trim(),
        IsDefaultForContext = dto.IsDefaultForContext,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void UpdateEntity(IdentityProfile entity, UpdateIdentityProfileDto dto)
    {
        entity.DisplayName = dto.DisplayName.Trim();
        entity.Context = dto.Context;
        entity.BirthDate = dto.BirthDate;
        entity.Title = dto.Title?.Trim();
        entity.Email = dto.Email?.Trim();
        entity.Phone = dto.Phone?.Trim();
        entity.Address = dto.Address?.Trim();
        entity.IsDefaultForContext = dto.IsDefaultForContext;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}