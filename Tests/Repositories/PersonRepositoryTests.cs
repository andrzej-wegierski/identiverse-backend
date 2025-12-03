using Database;
using Database.Factories;
using Database.Repositories;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories;

public class PersonRepositoryTests
{
    private static IdentiverseDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<IdentiverseDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new IdentiverseDbContext(options);
    }

    private static PersonRepository CreateRepo(IdentiverseDbContext db)
        => new(db, new PersonFactory());

    [Test]
    public async Task Create_And_Get_By_Id_Works()
    {
        await using var db = CreateDb(nameof(Create_And_Get_By_Id_Works));
        var repo = CreateRepo(db);

        var before = DateTime.UtcNow.AddSeconds(-1);
        var created = await repo.CreatePersonAsync(new CreatePersonDto
        {
            FirstName = "  John ",
            LastName = " Doe  ",
            PreferredName = " JD "
        });
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(created.ExternalId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.FirstName, Is.EqualTo("John"));
        Assert.That(created.LastName, Is.EqualTo("Doe"));
        Assert.That(created.PreferredName, Is.EqualTo("JD"));
        Assert.That(created.CreatedAt, Is.InRange(before, after));
        Assert.That(created.UpdatedAt, Is.InRange(before, after));

        var fetched = await repo.GetPersonByIdAsync(created.Id);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task GetPersons_Returns_All()
    {
        await using var db = CreateDb(nameof(GetPersons_Returns_All));
        var repo = CreateRepo(db);

        await repo.CreatePersonAsync(new CreatePersonDto { FirstName = "A", LastName = "B" });
        await repo.CreatePersonAsync(new CreatePersonDto { FirstName = "C", LastName = "D" });

        var list = await repo.GetPersonsAsync();
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetById_Returns_Null_When_Missing()
    {
        await using var db = CreateDb(nameof(GetById_Returns_Null_When_Missing));
        var repo = CreateRepo(db);
        var res = await repo.GetPersonByIdAsync(999);
        Assert.That(res, Is.Null);
    }

    [Test]
    public async Task Update_Returns_Updated_Dto_And_Changes_Timestamp()
    {
        await using var db = CreateDb(nameof(Update_Returns_Updated_Dto_And_Changes_Timestamp));
        var repo = CreateRepo(db);
        var created = await repo.CreatePersonAsync(new CreatePersonDto { FirstName = "X", LastName = "Y" });

        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
        var updated = await repo.UpdatePersonAsync(created.Id, new UpdatePersonDto
        {
            FirstName = "NewFirst",
            LastName = "NewLast",
            PreferredName = "NewPref"
        });
        var afterUpdate = DateTime.UtcNow.AddSeconds(1);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.FirstName, Is.EqualTo("NewFirst"));
        Assert.That(updated.LastName, Is.EqualTo("NewLast"));
        Assert.That(updated.PreferredName, Is.EqualTo("NewPref"));
        Assert.That(updated.UpdatedAt, Is.InRange(beforeUpdate, afterUpdate));
    }

    [Test]
    public async Task Update_Returns_Null_When_Not_Found()
    {
        await using var db = CreateDb(nameof(Update_Returns_Null_When_Not_Found));
        var repo = CreateRepo(db);
        var res = await repo.UpdatePersonAsync(123, new UpdatePersonDto { FirstName = "A", LastName = "B" });
        Assert.That(res, Is.Null);
    }

    [Test]
    public async Task Delete_Returns_Flags_And_Removes()
    {
        await using var db = CreateDb(nameof(Delete_Returns_Flags_And_Removes));
        var repo = CreateRepo(db);

        var created = await repo.CreatePersonAsync(new CreatePersonDto { FirstName = "A", LastName = "B" });
        var ok = await repo.DeletePersonAsync(created.Id);
        Assert.That(ok, Is.True);
        Assert.That(await repo.GetPersonByIdAsync(created.Id), Is.Null);

        var missing = await repo.DeletePersonAsync(999);
        Assert.That(missing, Is.False);
    }
}
