using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Domain.Exceptions;

namespace Domain.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto user, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly JwtOptions _jwt;

    public AuthService(IUserRepository repo, IOptions<JwtOptions> jwtOptions)
    {
        _repo = repo;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto user, CancellationToken ct = default)
    {
        if (await _repo.IsUsernameTakenAsync(user.Username, ct))
            throw new ConflictException("Username is already taken");
        
        if (await _repo.IsEmailTakenAsync(user.Email, ct))
            throw new ConflictException("Email is already registered");
        
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashPassword(user.Password, salt);

        var newUserId = await _repo.RegisterUserAsync(user, hash, salt, ct);

        var userDto = await _repo.GetByIdAsync(newUserId, ct) ??
                      throw new NotFoundException("User not found after create");
        
        return CreateAuthResponse(userDto);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default)
    {
        var auth = await _repo.GetAuthByUserNameOrEmailAsync(user.UsernameOrEmail, ct);
        if (auth is null)
            throw new UnauthorizedIdentiverseException("Invalid credentials");
        
        var salt = Convert.FromBase64String(auth.PasswordSalt);
        var computed = Convert.ToBase64String(HashPassword(user.Password, salt));
        var expected = auth.PasswordHash;
        
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(computed)))
            throw new UnauthorizedIdentiverseException("Invalid credentials");
        
        return CreateAuthResponse(auth.User);
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