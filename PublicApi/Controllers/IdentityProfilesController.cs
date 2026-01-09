using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[Authorize(Policy = "SelfOrAdmin")]
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
        var dto = await _service.GetProfileByIdForPersonAsync(personId, identityId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<IdentityProfileDto>> CreateProfile([FromRoute] int personId,
        [FromBody] CreateIdentityProfileDto request, CancellationToken ct = default)
    {
        var created = await _service.CreateProfileAsync(personId, request, ct);
        return CreatedAtAction(nameof(GetProfileById), new { personId = personId, identityId = created.Id }, created);
    }

    [HttpPut("{identityId:int}")]
    public async Task<ActionResult<IdentityProfileDto>> UpdateProfile([FromRoute] int personId, [FromRoute] int identityId,
        [FromBody] UpdateIdentityProfileDto request, CancellationToken ct = default)
    {
        var existing = await _service.GetProfileByIdForPersonAsync(personId, identityId, ct);
        if (existing is null)
            return NotFound();

        var updated = await _service.UpdateProfileAsync(identityId, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
    
    [HttpDelete("{identityId:int}")]
    public async Task<ActionResult> DeleteProfile([FromRoute] int personId, [FromRoute] int identityId, CancellationToken ct = default)
    {
        var existing = await _service.GetProfileByIdForPersonAsync(personId, identityId, ct);
        if (existing is null)
            return NotFound();

        var deleted = await _service.DeleteProfileAsync(identityId, ct);
        return !deleted ? NotFound() : NoContent();
    }
    
    [HttpGet("preferred")]
    public async Task<ActionResult<IdentityProfileDto?>> GetPreferredProfile([FromRoute] int personId, [FromQuery] IdentityContext context, CancellationToken ct = default)
    {
        var dto = await _service.GetPreferredProfileAsync(personId, context, ct);
        if (dto is null) 
            return NotFound();
        
        return Ok(dto);
    }

    [HttpPut("{identityId:int}/default")]
    public async Task<ActionResult> SetDefaultProfile([FromRoute] int personId, [FromRoute] int identityId,
        CancellationToken ct = default)
    {
        var result = await _service.SetDefaultProfileAsync(personId, identityId, ct);
        return result ? NoContent() : NotFound();
    }
}