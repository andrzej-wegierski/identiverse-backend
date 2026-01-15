using Domain.Abstractions;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserContext _currentUser;

    public AuthController(IAuthService auth, ICurrentUserContext currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken ct)
    {
        await _auth.RegisterAsync(dto, ct);
        return Created("", new { Message = "Please confirm your email" });
    } 
    
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginUserDto dto, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(dto, ct);
        return Ok(result);
    }
    
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(dto, ct);
        return Ok(new { Message = "Please check your email for password reset link" });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(dto, ct);
        return Ok(new { Message = "Password has been reset successfully!" });
    }

    [HttpPost("resend-confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationDto dto, CancellationToken ct)
    {
        try
        {
            await _auth.ResendConfirmationEmailAsync(dto, ct);
            return Ok(new { Message = "Please check your email for confirmation link" });
        }
        catch (ConflictException ex)
        {
            return Ok(new { Message = ex.Message });
        }
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto, CancellationToken ct)
    {
        var confirmed = await _auth.ConfirmEmailAsync(dto, ct);
        return Ok(new
        {
            Message = confirmed
                ? "Email confirmed successfully. You can now log in"
                : "Email is already confirmed. You can log in"
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        await _auth.ChangePassword(_currentUser.UserId, dto, ct);
        return Ok(new { Message = "Password changed successfully!" });
    }
}