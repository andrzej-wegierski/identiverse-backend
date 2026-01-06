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
        var users = await _users.Users
            .AsNoTracking()
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                PersonId = u.PersonId,
                Roles = new List<string>()
            })
            .ToListAsync(ct);

        foreach (var userDto in users)
        {
            var userEntity = new ApplicationUser { Id = userDto.Id };
            var roles = await _users.GetRolesAsync(userEntity);
            userDto.Roles.AddRange(roles);
        }

        return Ok(users);
    }

    [HttpGet("persons")]
    public async Task<ActionResult<List<PersonDto>>> GetAllPersons(CancellationToken ct)
    {
        var list = await _persons.GetPersonsAsync(ct);
        return Ok(list);
    }
}