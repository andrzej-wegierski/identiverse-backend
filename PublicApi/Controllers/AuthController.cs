using Domain.Abstractions;
using Domain.Models;
using Domain.Services;
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
        // todo remove: Temporary call for verification
        await _emailSender.SendEmailAsync(dto.Email, "Email confirmation", "Please confirm your email address");
        
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
}