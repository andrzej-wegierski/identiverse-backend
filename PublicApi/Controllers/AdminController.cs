using Database.Entities;
using Domain.Abstractions;
using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace identiverse_backend.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IPersonService _persons;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(IPersonService persons, UserManager<ApplicationUser> users)
    {
        _persons = persons;
        _users = users;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers(CancellationToken ct = default)
    {
        var list = await _users.Users.ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("persons")]
    public async Task<ActionResult<List<PersonDto>>> GetAllPersons(CancellationToken ct)
    {
        var list = await _persons.GetPersonsAsync(ct);
        return Ok(list);
    }
}