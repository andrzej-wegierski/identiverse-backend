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
    private readonly IUserRepository _users;
    private readonly JwtOptions _jwt;
    private readonly PasswordOptions _pwd;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly ILoginThrottle _throttle;

    public AuthService(
        IUserRepository users,
        IOptions<JwtOptions> jwtOptions,
        IOptions<PasswordOptions> passwordOptions,
        IPasswordPolicy passwordPolicy,
        ILoginThrottle throttle)
    {
        _users = users;
        _jwt = jwtOptions.Value;
        _pwd = passwordOptions.Value;
        _passwordPolicy = passwordPolicy;
        _throttle = throttle;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto user, CancellationToken ct = default)
    {
        if (await _users.IsUsernameTakenAsync(user.Username, ct))
            throw new ConflictException("Username is already taken");
        
        if (await _users.IsEmailTakenAsync(user.Email, ct))
            throw new ConflictException("Email is already registered");
        
        _passwordPolicy.Validate(user.Password);

        var salt = RandomNumberGenerator.GetBytes(_pwd.SaltSize);
        var hash = HashPassword(user.Password, salt);

        var newUserId = await _users.RegisterUserAsync(user, hash, salt, ct);

        var userDto = await _users.GetByIdAsync(newUserId, ct) ??
                      throw new NotFoundException("User not found after create");
        
        return CreateAuthResponse(userDto);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default)
    {
        var key = user.UsernameOrEmail.Trim().ToLowerInvariant();
        if (!await _throttle.IsAllowedAsync(key, ct))
            throw new TooManyRequestsException("Too many login attempts. Please try again shortly.");

        var auth = await _users.GetAuthByUserNameOrEmailAsync(user.UsernameOrEmail, ct);
        if (auth is null)
        {
            await _throttle.RegisterFailureAsync(key, ct);
            throw new UnauthorizedIdentiverseException("Invalid credentials");
        }
        
        var salt = Convert.FromBase64String(auth.PasswordSalt);
        var computed = Convert.ToBase64String(HashPassword(user.Password, salt));
        var expected = auth.PasswordHash;
        
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(computed)))
        {
            await _throttle.RegisterFailureAsync(key, ct);
            throw new UnauthorizedIdentiverseException("Invalid credentials");
        }
        
        await _throttle.RegisterSuccessAsync(key, ct);
        return CreateAuthResponse(auth.User);
    }

    private byte[] HashPassword(string password, byte[] salt)
    {
        var iterations = _pwd.Iterations;
        var keySize = _pwd.KeySize;
        var algo = _pwd.HashAlgorithm?.ToUpperInvariant() switch
        {
            "SHA512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algo, keySize);
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