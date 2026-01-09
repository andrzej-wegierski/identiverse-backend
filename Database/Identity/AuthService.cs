using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Database.Entities;
using Domain.Abstractions;
using Domain.Exceptions;
using Domain.Models;
using Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Database.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtOptions _jwt;
    private readonly ILoginThrottle _throttle;
    private readonly IEmailThrottle _emailThrottle;
    private readonly IEmailSender _emailSender;
    private readonly FrontendLinksOptions _links;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptions<JwtOptions> jwtOptions,
        ILoginThrottle throttle,
        IEmailThrottle emailThrottle,
        IEmailSender emailSender,
        IOptions<FrontendLinksOptions> linksOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwtOptions.Value;
        _throttle = throttle;
        _emailThrottle = emailThrottle;
        _emailSender = emailSender;
        _links = linksOptions.Value;
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
        
        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
        var link = GenerateLink(_links.ConfirmEmailPath, appUser.Email!, confirmToken);
        
        await _emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your email",
            $"Please confirm your account by clicking here: {link}");
        
        var userDto = await MapToDtoAsync(appUser);
        return CreateAuthResponse(userDto);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto user, CancellationToken ct = default)
    {
        if (!await _throttle.IsAllowedAsync(user.UsernameOrEmail, ct))
            throw new TooManyRequestsException("Too many login attempts. Please try again later.");
        
        var appUser = await _userManager.FindByNameAsync(user.UsernameOrEmail)
            ?? await _userManager.FindByEmailAsync(user.UsernameOrEmail);

        if (appUser is null)
        {
            await _throttle.RegisterFailureAsync(user.UsernameOrEmail, ct);
            throw new UnauthorizedIdentiverseException("Invalid username or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(appUser, user.Password, true);
        
        if (result.IsLockedOut)
            throw new ForbiddenException("User is locked due to multiple failed login attempts.");

        if (!result.Succeeded)
        {
            await _throttle.RegisterFailureAsync(user.UsernameOrEmail, ct);
            throw new UnauthorizedIdentiverseException("Invalid username or password");
        }

        if (!await _userManager.IsEmailConfirmedAsync(appUser))
        {
            await _throttle.RegisterFailureAsync(user.UsernameOrEmail, ct);
            throw new EmailNotConfirmedException();
        }
        
        await _throttle.RegisterSuccessAsync(user.UsernameOrEmail, ct);
        
        var userDto = await MapToDtoAsync(appUser);
        return CreateAuthResponse(userDto);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        if (!await _emailThrottle.IsAllowedAsync(dto.Email, ct))
            return;
        
        await _emailThrottle.RegisterAttemptAsync(dto.Email, ct);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !(await _userManager.IsEmailConfirmedAsync(user)))
            return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var link = GenerateLink(_links.ResetPasswordPath, user.Email!, token);
        
        await _emailSender.SendEmailAsync(
            user.Email!,
            "Reset Password",
            $"Reset your password by clicking here: {link}");
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            throw new ValidationException("Invalid request");

        var decodedToken = DecodeToken(dto.Token);

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
        if (!result.Succeeded)
            throw new ValidationException("Failed to reset password.");
    }

    public async Task ResendConfirmationEmailAsync(ResendConfirmationDto dto, CancellationToken ct = default)
    {
        if (!await _emailThrottle.IsAllowedAsync(dto.Email, ct))
            return;

        await _emailThrottle.RegisterAttemptAsync(dto.Email, ct);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return;

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            throw new ConflictException("Email is already confirmed");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = GenerateLink(_links.ConfirmEmailPath, user.Email!, token);
        
        await _emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your email",
            $"Please confirm your account by clicking here: {link}");
    }

    public async Task<bool> ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken ct = default)
    { 
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            throw new ValidationException("Invalid request");

        if (await _userManager.IsEmailConfirmedAsync(user))
            return false;

        var decodedToken = DecodeToken(dto.Token);
        
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            throw new ValidationException("Failed to confirm email.");

        return true;
    }

    public async Task ChangePassword(int userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new UnauthorizedIdentiverseException("User not found");
        
        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault();
            if (firstError?.Code == "PasswordMismatch")
                throw new UnauthorizedIdentiverseException("Invalid current password");
            
            throw new ValidationException(firstError?.Description ?? "Failed to change password");
        }
        
        await _userManager.UpdateSecurityStampAsync(user);
    }

    private string DecodeToken(string token)
    {
        try
        {
            var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
            return Encoding.UTF8.GetString(decodedTokenBytes);
        }
        catch (Exception)
        {
            throw new ValidationException("Invalid request");
        }
    }

    private async Task<UserDto> MapToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "User";
        
        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = Enum.TryParse<Domain.Enums.UserRole>(primaryRole, ignoreCase: true, out var role) ? role : Domain.Enums.UserRole.User,
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

    private string GenerateLink(string path, string email, string token)
    {
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        
        var uriBuilder = new UriBuilder(_links.BaseUrl)
        {
            Path = path,
            Query = $"email={Uri.EscapeDataString(email)}&token={encodedToken}"
        };

        return uriBuilder.ToString();
    }

}