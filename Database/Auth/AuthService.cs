using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Database.Entities;
using Domain.Exceptions;
using Domain.Security;
using Microsoft.AspNetCore.Identity;
using PasswordOptions = Domain.Models.PasswordOptions;

namespace Domain.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwt;
    private readonly ILoginThrottle _throttle;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        IOptions<JwtOptions> jwtOptions,
        ILoginThrottle throttle)
    {
        _userManager = userManager;
        _jwt = jwtOptions.Value;
        _throttle = throttle;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto user, CancellationToken ct = default)
    {
        var appUser = new ApplicationUser
        {
            UserName = user.Username, 
            Email = user.Email,
            PersonId = user.PersonId
        };
        
        var result = await _userManager.CreateAsync(appUser, user.Password);

        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault();
            if (firstError?.Code == "DuplicateUserName")
                throw new ConflictException("Username already exists");
            
            if (firstError?.Code == "DuplicateEmail")
                throw new ConflictException("Email already exists");

            throw new ValidationException(firstError?.Description ?? "User registration failed.");
        }
        
        await _userManager.AddToRoleAsync(appUser, "User");
        
        var userDto = MapToDto(appUser, "User");
        return CreateAuthResponse(userDto);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default)
    {
        // todo implement this!
        throw new NotImplementedException();
    }

    private static UserDto MapToDto(ApplicationUser user, string role)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = Enum.Parse<Enums.UserRole>(role),
            PersonId = user.PersonId
        };
    }

    private AuthResponseDto CreateAuthResponse(UserDto user)
    {
        var key = new SymmetricSecurityKey(JwtKeyParser.GetSigningKeyBytes(_jwt.SigningKey));
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