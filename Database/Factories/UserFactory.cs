using Database.Entities;
using Domain.Models;

namespace Database.Factories;

public interface IUserFactory
{
    UserDto ToDto(User entity);
    User FromDto(UserDto dto);
    User FromRegisterDto(RegisterUserDto dto, byte[] hash, byte[] salt);
}
     
public class UserFactory : IUserFactory
{
    public UserDto ToDto(User entity) => new() 
    {
        Id =  entity.Id,
        Username = entity.Username,
        Email = entity.Email,
        Role = entity.Role,
        PersonId = entity.PersonId
    };

    public User FromDto(UserDto dto) => new()
    {
        Id = dto.Id,
        Username = dto.Username,
        Email = dto.Email,
        Role = dto.Role,
        PersonId = dto.PersonId
    };

    public User FromRegisterDto(RegisterUserDto dto, byte[] hash, byte[] salt) => new()
    {
        Username = dto.Username,
        Email = dto.Email,
        PasswordHash = Convert.ToBase64String(hash),
        PasswordSalt = Convert.ToBase64String(salt),
        Role = "User",
        PersonId = dto.PersonId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

}