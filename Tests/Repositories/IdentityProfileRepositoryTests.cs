using Database;
using Database.Entities;
using Database.Factories;
using Database.Repositories;
using Domain.Enums;
using Domain.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories;

public class IdentityProfileRepositoryTests
{
    private static IdentiverseDbContext CreateDbContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<IdentiverseDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new IdentiverseDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static Person SeedPerson(IdentiverseDbContext db)
    {
        var p = new Person
        {
            FirstName = "Alice",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Persons.Add(p);
        db.SaveChanges();
        return p;
    }

    [Test]
    public async Task CreateAsync_Persists_And_Returns_Dto()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var person = SeedPerson(db);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        var created = await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto
        {
            DisplayName = "Alice (Work)", Context = IdentityContext.Legal, Language = "en-GB", IsDefaultForContext = true
        });

        Assert.That(created.Id, Is.GreaterThan(0));
        var entity = await db.IdentityProfiles.FindAsync(created.Id);
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity!.DisplayName, Is.EqualTo("Alice (Work)"));
        Assert.That(entity.IsDefaultForContext, Is.True);
    }

    [Test]
    public async Task GetProfileByIdAsync_Returns_Null_When_NotFound()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        var dto = await repo.GetProfileByIdAsync(999);
        Assert.That(dto, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_Changes_Fields_And_Timestamp()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var person = SeedPerson(db);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        var created = await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto
        {
            DisplayName = "Alice (Work)", Context = IdentityContext.Legal, Language = "en-GB", IsDefaultForContext = false
        });

        var before = await db.IdentityProfiles.AsNoTracking().FirstAsync(x => x.Id == created.Id);
        var updated = await repo.UpdateProfileAsync(created.Id, new UpdateIdentityProfileDto
        {
            DisplayName = "Alice W",
            Context = IdentityContext.Legal,
            Language = "nb-NO",
            IsDefaultForContext = true
        });

        Assert.That(updated, Is.Not.Null);
        var after = await db.IdentityProfiles.AsNoTracking().FirstAsync(x => x.Id == created.Id);
        Assert.That(after.DisplayName, Is.EqualTo("Alice W"));
        Assert.That(after.Language, Is.EqualTo("nb-NO"));
        Assert.That(after.IsDefaultForContext, Is.True);
        Assert.That(after.UpdatedAt, Is.GreaterThanOrEqualTo(before.UpdatedAt));
    }

    [Test]
    public async Task DeleteAsync_Removes_Entity_And_Returns_True()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var person = SeedPerson(db);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        var created = await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto { DisplayName = "X", Context = IdentityContext.Legal });
        var ok = await repo.DeleteProfileAsync(created.Id);

        Assert.That(ok, Is.True);
        var fromDb = await db.IdentityProfiles.FindAsync(created.Id);
        Assert.That(fromDb, Is.Null);
    }

    [Test]
    public async Task GetProfilesByPersonAsync_Returns_Sorted_List()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var person = SeedPerson(db);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        // Different contexts and defaults to exercise ordering
        await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto { DisplayName = "B", Context = IdentityContext.Legal, IsDefaultForContext = false });
        await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto { DisplayName = "A", Context = IdentityContext.Legal, IsDefaultForContext = true });
        await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto { DisplayName = "Z", Context = IdentityContext.Social, IsDefaultForContext = false });

        var list = await repo.GetProfilesByPersonAsync(person.Id);
        
        Assert.That(list.Select(x => (x.Context, x.DisplayName)).ToList(), Is.EqualTo(new List<(IdentityContext, string)>
        {
            (IdentityContext.Legal, "A"),
            (IdentityContext.Legal, "B"),
            (IdentityContext.Social, "Z"),
        }));
    }
}