using Domain.Abstractions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IEmailSender _emailSender;

    public AuthController(IAuthService auth, IEmailSender emailSender)
    {
        _auth = auth;
        _emailSender = emailSender;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserDto dto, CancellationToken ct)
    {
        var result = await  _auth.RegisterAsync(dto, ct);
        return Ok(result);
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
        await _auth.ResendConfirmationEmailAsync(dto, ct);
        return Ok(new { Message = "Please check your email for confirmation link" });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto, CancellationToken ct)
    {
        await _auth.ConfirmEmailAsync(dto, ct);
        return Ok(new { Message = "Email confirmed successfully. You can now log in" });
    }
}