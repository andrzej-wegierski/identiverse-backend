using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[Authorize]
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
    public async Task<ActionResult<List<IdentityProfileDto>>> GetProfilesForPerson([FromRoute] int personId, CancellationToken ct = default)
    {
        var list = await _service.GetProfilesByPersonAsync(personId, ct);
        return Ok(list);
    }

    [HttpGet("{identityId:int}", Name = "GetIdentityProfileById")]
    public async Task<ActionResult<IdentityProfileDto>> GetProfileById([FromRoute] int personId, [FromRoute] int identityId, CancellationToken ct = default)
    {
        var dto = await _service.GetProfileByIdAsync(identityId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<IdentityProfileDto>> CreateProfile([FromRoute] int personId,
        [FromBody] CreateIdentityProfileDto request, CancellationToken ct = default)
    {
        var created = await _service.CreateProfileAsync(personId, request, ct);
        return CreatedAtAction(nameof(GetProfileById), new { id = created.Id, personId = personId, identityId = created.Id }, created);
    }

    [HttpPut("{identityId:int}")]
    public async Task<ActionResult<IdentityProfileDto>> UpdateProfile([FromRoute] int personId, [FromRoute] int identityId,
        [FromBody] UpdateIdentityProfileDto request, CancellationToken ct = default)
    {
        var updated = await _service.UpdateProfileAsync(identityId, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
    
    [HttpDelete("{identityId:int}")]
    public async Task<ActionResult> DeleteProfile([FromRoute] int personId, [FromRoute] int identityId, CancellationToken ct = default)
    {
        var deleted = await _service.DeleteProfileAsync(identityId, ct);
        return !deleted ? NotFound() : NoContent();
    }
    
    [HttpGet("preferred")]
    public async Task<ActionResult<IdentityProfileDto?>> GetPreferredProfile([FromRoute] int personId, [FromQuery] IdentityContext context, [FromQuery] string? language, CancellationToken ct = default)
    {
        var dto = await _service.GetPreferredProfileAsync(personId, context, language, ct);
        if (dto is null) 
            return NotFound();
        
        return Ok(dto);
    }
}