using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[ApiController]
[Route("persons/{personId:int}/identities")]
public class IdentityProfilesController : ControllerBase
{
    private readonly IIdentityProfileService _service;

    public IdentityProfilesController(IIdentityProfileService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<IdentityProfileDto>>> GetProfilesForPerson(int personId, CancellationToken ct)
    {
        var list = await _service.GetProfilesByPersonAsync(personId, ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IdentityProfileDto>> GetProfileById(int id, CancellationToken ct)
    {
        var dto = await _service.GetProfileByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<IdentityProfileDto>> CreateProfile(int personId,
        [FromBody] CreateIdentityProfileDto request, CancellationToken ct)
    {
        var created = await _service.CreateProfileAsync(personId, request, ct);
        return CreatedAtAction(nameof(GetProfileById), new { id = created.Id }, created);
    }

    [HttpPut("({identityId:int})")]
    public async Task<ActionResult<IdentityProfileDto>> UpdateProfile(int personId, int identityId,
        [FromBody] UpdateIdentityProfileDto request, CancellationToken ct)
    {
        var existing = await _service.GetProfileByIdAsync(identityId, ct);
        if (existing is null) 
            return NotFound();
        
        var updated = await _service.UpdateProfileAsync(identityId, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
    
    [HttpDelete("({identityId:int})")]
    public async Task<ActionResult> DeleteProfile(int personId, int identityId, CancellationToken ct)
    {
        var deleted = await _service.DeleteProfileAsync(identityId, ct);
        return !deleted ? NotFound() : NoContent();
    }
    
    [HttpGet("preferred")]
    public async Task<ActionResult<IdentityProfileDto?>> GetPreferredProfile(int personId, [FromQuery] string context, [FromQuery] string? language, CancellationToken ct )
    {
        var dto = await _service.GetPreferredProfileAsync(personId, context, language, ct);
        if (dto is null) 
            return NotFound();
        
        return Ok(dto);
    }
}