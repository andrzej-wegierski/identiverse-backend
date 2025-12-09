using Domain.Models;
using Domain.Services;
using identiverse_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[Authorize]
[ApiController]
[Route("/persons")]
public class PersonsController : ControllerBase
{
    private readonly IPersonService _service;
    private readonly ICurrentUserService _user;

    public PersonsController(IPersonService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<PersonDto>>> GetPersons(CancellationToken ct = default)
    {
        if (!_user.IsAdmin)
            return Forbid();
        
        var list = await _service.GetPersonsAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PersonDto>> GetPersonById(int id, CancellationToken ct = default)
    {
        if (!AuthorizationHelpers.CanAccessPerson(_user, id))
            return Forbid();
        
        var dto = await _service.GetPersonByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<PersonDto>> CreatePerson([FromBody] CreatePersonDto request, CancellationToken ct = default)
    {
        if (_user.IsAdmin)
        {
            var createdAdmin = await _service.CreatePersonAsync(request, ct);
            return CreatedAtAction(nameof(GetPersonById), new {id = createdAdmin.Id}, createdAdmin);    
        }
        
        var user = _user.GetCurrentUser();
        if (user is null)
            return Unauthorized();
        if (user.PersonId is not null)
            return Conflict("User already has a person");
        
        var created = await _service.CreatePersonAsync(request, ct);
        return CreatedAtAction(nameof(GetPersonById), new {id = created.Id}, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PersonDto>> UpdatePerson(int id, [FromBody] UpdatePersonDto request,
        CancellationToken ct = default)
    {
        if (!AuthorizationHelpers.CanAccessPerson(_user, id))
            return Forbid();
        
        var updated = await _service.UpdatePersonAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeletePerson(int id, CancellationToken ct = default)
    {
        if (!_user.IsAdmin)
            return Forbid();
        
        var deleted = await _service.DeletePersonAsync(id, ct);
        return !deleted ? NotFound() : NoContent();
    }
}