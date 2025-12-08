using Database.Entities;
using Database.Factories;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentiverseDbContext _db;
    private readonly IUserFactory _factory;

    public UserRepository(IdentiverseDbContext db, IUserFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Users.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null :  _factory.ToDto(entity);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(p => p.Username == username, ct);
        return entity is null ? null : _factory.ToDto(entity);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(p => p.Email == email, ct);
        return entity is null ? null : _factory.ToDto(entity);
    }

    public async Task<int> RegisterUserAsync(RegisterUserDto user, byte[] passwordHash, byte[] passwordSalt, CancellationToken ct = default)
    {
        var entity = _factory.FromRegisterDto(user, passwordHash, passwordSalt);
        await _db.Users.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<AuthUserData?> GetAuthByUserNameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default)
    {
        var entity = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail, ct);
        if (entity is null)
            return null;

        return new AuthUserData
        {
            User = _factory.ToDto(entity),
            PasswordHash = entity.PasswordHash,
            PasswordSalt = entity.PasswordSalt
        };
    }

    public Task<bool> IsUsernameTakenAsync(string username, CancellationToken ct = default)
        => _db.Users.AnyAsync(p => p.Username == username, ct);

    public Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default)
        => _db.Users.AnyAsync(p => p.Email == email, ct);
}