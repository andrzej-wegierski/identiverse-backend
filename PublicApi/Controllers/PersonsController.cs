using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[Authorize]
[ApiController]
[Route("persons")]
public class PersonsController : ControllerBase
{
    private readonly IPersonService _service;

    public PersonsController(IPersonService service)
    {
        _service = service;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<List<PersonDto>>> GetPersons(CancellationToken ct = default)
    {
        var list = await _service.GetPersonsAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "SelfOrAdmin")]
    public async Task<ActionResult<PersonDto>> GetPersonById(int id, CancellationToken ct = default)
    {
        var dto = await _service.GetPersonByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<PersonDto>> CreatePerson([FromBody] CreatePersonDto request, CancellationToken ct = default)
    {
        var created = await _service.CreatePersonForCurrentUserAsync(request, ct);
        return CreatedAtAction(nameof(GetPersonById), new {id = created.Id}, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "SelfOrAdmin")]
    public async Task<ActionResult<PersonDto>> UpdatePerson(int id, [FromBody] UpdatePersonDto request,
        CancellationToken ct = default)
    {
        var updated = await _service.UpdatePersonAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "SelfOrAdmin")]
    public async Task<ActionResult> DeletePerson(int id, CancellationToken ct = default)
    {
        var deleted = await _service.DeletePersonAsync(id, ct);
        return !deleted ? NotFound() : NoContent();
    }
}