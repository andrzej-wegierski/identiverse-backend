using Database;
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

    public AdminController(IPersonService persons)
    {
        _persons = persons;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetAllUsers(
        [FromServices] IdentiverseDbContext dbContext,
        CancellationToken ct = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                Username = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                u.PersonId
            })
            .ToListAsync(ct);

        var rolesByUserId = await (
            from ur in dbContext.UserRoles.AsNoTracking()
            join r in dbContext.Roles.AsNoTracking() on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.Name! }
        ).ToListAsync(ct);

        var rolesLookup = rolesByUserId
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).Distinct().ToList());

        var result = users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            PersonId = u.PersonId,
            Roles = rolesLookup.TryGetValue(u.Id, out var roles) ? roles : new List<string>()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("persons")]
    public async Task<ActionResult<List<PersonDto>>> GetAllPersons(CancellationToken ct)
    {
        var list = await _persons.GetPersonsAsync(ct);
        return Ok(list);
    }
}