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
            UpdatedAt = DateTime.UtcNow,
            ExternalId = Guid.NewGuid()
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

        var birthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var created = await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto
        {
            DisplayName = "Alice (Work)", 
            Context = IdentityContext.Legal, 
            IsDefaultForContext = true,
            BirthDate = birthDate
        });

        Assert.That(created.Id, Is.GreaterThan(0));
        var entity = await db.IdentityProfiles.FindAsync(created.Id);
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity!.DisplayName, Is.EqualTo("Alice (Work)"));
        Assert.That(entity.IsDefaultForContext, Is.True);
        Assert.That(entity.BirthDate, Is.EqualTo(DateTime.SpecifyKind(birthDate, DateTimeKind.Utc)));
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
            DisplayName = "Alice (Work)", Context = IdentityContext.Legal, IsDefaultForContext = false
        });

        var before = await db.IdentityProfiles.AsNoTracking().FirstAsync(x => x.Id == created.Id);
        var newBirthDate = new DateTime(1995, 5, 5, 0, 0, 0, DateTimeKind.Utc);
        var updated = await repo.UpdateProfileAsync(created.Id, new UpdateIdentityProfileDto
        {
            DisplayName = "Alice W",
            Context = IdentityContext.Legal,
            IsDefaultForContext = true,
            BirthDate = newBirthDate
        });

        Assert.That(updated, Is.Not.Null);
        var after = await db.IdentityProfiles.AsNoTracking().FirstAsync(x => x.Id == created.Id);
        Assert.That(after.DisplayName, Is.EqualTo("Alice W"));
        Assert.That(after.IsDefaultForContext, Is.True);
        Assert.That(after.BirthDate, Is.EqualTo(newBirthDate));
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
        
        Assert.That(list.Select(x => (x.Context, x.DisplayName, x.IsDefaultForContext)).ToList(), Is.EqualTo(new List<(IdentityContext, string, bool)>
        {
            (IdentityContext.Legal, "A", true),
            (IdentityContext.Legal, "B", false),
            (IdentityContext.Social, "Z", false),
        }));
    }

    [Test]
    public async Task GetProfilesByPersonAsync_Orders_By_DisplayName_When_No_Default()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var person = SeedPerson(db);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto { DisplayName = "Z", Context = IdentityContext.Legal, IsDefaultForContext = false });
        await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto { DisplayName = "A", Context = IdentityContext.Legal, IsDefaultForContext = false });
        await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto { DisplayName = "M", Context = IdentityContext.Legal, IsDefaultForContext = false });

        var list = await repo.GetProfilesByPersonAsync(person.Id);

        Assert.That(list.Select(x => x.DisplayName).ToList(), Is.EqualTo(new List<string> { "A", "M", "Z" }));
    }

    [Test]
    public async Task GetPersonIdByProfileIdAsync_Returns_Null_When_NotFound()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        var personId = await repo.GetPersonIdByProfileIdAsync(999);
        Assert.That(personId, Is.Null);
    }

    [Test]
    public async Task SetAsDefaultAsync_Sets_Target_And_Unsets_Others_In_Context()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var person = SeedPerson(db);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        // Create two profiles for same context
        var p1 = await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto 
        { 
            DisplayName = "P1", 
            Context = IdentityContext.Social, 
            IsDefaultForContext = true 
        });
        var p2 = await repo.CreateProfileAsync(person.Id, new CreateIdentityProfileDto 
        { 
            DisplayName = "P2", 
            Context = IdentityContext.Social, 
            IsDefaultForContext = false 
        });

        // Act: Set P2 as default
        var result = await repo.SetAsDefaultAsync(p2.Id);

        // Assert
        Assert.That(result, Is.True);
        
        var e1 = await db.IdentityProfiles.AsNoTracking().FirstAsync(x => x.Id == p1.Id);
        var e2 = await db.IdentityProfiles.AsNoTracking().FirstAsync(x => x.Id == p2.Id);

        Assert.That(e1.IsDefaultForContext, Is.False, "P1 should be unset as default");
        Assert.That(e2.IsDefaultForContext, Is.True, "P2 should be set as default");
    }

    [Test]
    public async Task SetAsDefaultAsync_Returns_False_If_Profile_Not_Found()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateDbContext(conn);
        var factory = new IdentityProfileFactory();
        var repo = new IdentityProfileRepository(db, factory);

        var result = await repo.SetAsDefaultAsync(999);
        Assert.That(result, Is.False);
    }
}