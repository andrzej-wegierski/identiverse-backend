using Domain.Abstractions;
using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identiverse_backend.Controllers;

[ApiController]
[Route("/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPersonService _persons;

    public AdminController(IUserRepository users, IPersonService persons)
    {
        _users = users;
        _persons = persons;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetAllusers(CancellationToken ct = default)
    {
        var list = await _users.GetAllAsync(ct);
        return Ok(list);
    }

    [HttpGet("persons")]
    public async Task<ActionResult<List<PersonDto>>> GetAllPersons(CancellationToken ct)
    {
        var list = await _persons.GetPersonsAsync(ct);
        return Ok(list);
    }
}