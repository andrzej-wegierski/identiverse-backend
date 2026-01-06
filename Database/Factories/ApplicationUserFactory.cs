using Database.Entities;
using Domain.Models;

namespace Database.Factories;

public interface IApplicationUserFactory
{
    UserDto ToDto(ApplicationUser entity, string role);
}

public class ApplicationUserFactory : IApplicationUserFactory
{
    public UserDto ToDto(ApplicationUser entity, string role) => new()
    {
        Id = entity.Id,
        Username = entity.UserName ?? string.Empty,
        Email = entity.Email ?? string.Empty,
        PersonId = entity.PersonId
    };
}