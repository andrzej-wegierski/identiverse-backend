using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Domain.Exceptions;
using Microsoft.VisualBasic.CompilerServices;

namespace Domain.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto user, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default);
    Task<bool> CanAccessPersonAsync(int userId, bool isAdmin, int personId, CancellationToken ct = default);
    Task<bool> CanAccessIdentityProfileAsync(int userId, bool isAdmin, int profileId, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IIdentityProfileRepository _profiles;
    private readonly IPersonRepository _persons;
    private readonly JwtOptions _jwt;

    public AuthService(IUserRepository users, IOptions<JwtOptions> jwtOptions, IIdentityProfileRepository profiles, IPersonRepository persons)
    {
        _users = users;
        _profiles = profiles;
        _persons = persons;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto user, CancellationToken ct = default)
    {
        if (await _users.IsUsernameTakenAsync(user.Username, ct))
            throw new ConflictException("Username is already taken");
        
        if (await _users.IsEmailTakenAsync(user.Email, ct))
            throw new ConflictException("Email is already registered");
        
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashPassword(user.Password, salt);

        var newUserId = await _users.RegisterUserAsync(user, hash, salt, ct);

        var userDto = await _users.GetByIdAsync(newUserId, ct) ??
                      throw new NotFoundException("User not found after create");
        
        return CreateAuthResponse(userDto);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default)
    {
        var auth = await _users.GetAuthByUserNameOrEmailAsync(user.UsernameOrEmail, ct);
        if (auth is null)
            throw new UnauthorizedIdentiverseException("Invalid credentials");
        
        var salt = Convert.FromBase64String(auth.PasswordSalt);
        var computed = Convert.ToBase64String(HashPassword(user.Password, salt));
        var expected = auth.PasswordHash;
        
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(computed)))
            throw new UnauthorizedIdentiverseException("Invalid credentials");
        
        return CreateAuthResponse(auth.User);
    }

    public async Task<bool> CanAccessPersonAsync(int userId, bool isAdmin, int personId, CancellationToken ct = default)
    {
        if (isAdmin) return true;
        var ownerUserId = await _persons.GetUserIdByPersonIdAsync(personId, ct);
        return ownerUserId.HasValue && ownerUserId.Value == userId;
    }

    public async Task<bool> CanAccessIdentityProfileAsync(int userId, bool isAdmin, int profileId, CancellationToken ct = default)
    {
        if (isAdmin) return true;
        var personId = await _profiles.GetPersonIdByProfileIdAsync(profileId, ct);
        if (!personId.HasValue) return false;
        var ownerUserId = await _persons.GetUserIdByPersonIdAsync(personId.Value, ct);
        return ownerUserId.HasValue && ownerUserId.Value == userId;
        
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        const int iterations = 100_000; 
        const int keySize = 32;
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keySize);
    }

    private AuthResponseDto CreateAuthResponse(UserDto user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.PersonId.HasValue)
            claims.Add(new("personId", user.PersonId.Value.ToString()));
        
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            Expires = expires,
            User = user
        };
    } 
}