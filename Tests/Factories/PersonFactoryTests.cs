using Database.Entities;
using Database.Factories;
using Domain.Models;

namespace Tests.Factories;

public class PersonFactoryTests
{
    private readonly PersonFactory _factory = new();

    [Test]
    public void ToDto_Maps_All_Fields()
    {
        var now = DateTime.UtcNow;
        var entity = new Person
        {
            Id = 5,
            ExternalId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            PreferredName = "JD",
            // init-only and required
            CreatedAt = now,
            UpdatedAt = now
        };

        var dto = _factory.ToDto(entity);

        Assert.That(dto.Id, Is.EqualTo(entity.Id));
        Assert.That(dto.ExternalId, Is.EqualTo(entity.ExternalId));
        Assert.That(dto.FirstName, Is.EqualTo(entity.FirstName));
        Assert.That(dto.LastName, Is.EqualTo(entity.LastName));
        Assert.That(dto.PreferredName, Is.EqualTo(entity.PreferredName));
        Assert.That(dto.CreatedAt, Is.EqualTo(entity.CreatedAt));
        Assert.That(dto.UpdatedAt, Is.EqualTo(entity.UpdatedAt));
    }

    [Test]
    public void FromDto_Maps_All_Fields()
    {
        var now = DateTime.UtcNow;
        var dto = new PersonDto
        {
            Id = 7,
            ExternalId = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            PreferredName = "JS",
            CreatedAt = now,
            UpdatedAt = now
        };

        var entity = _factory.FromDto(dto);

        Assert.That(entity.Id, Is.EqualTo(dto.Id));
        Assert.That(entity.ExternalId, Is.EqualTo(dto.ExternalId));
        Assert.That(entity.FirstName, Is.EqualTo(dto.FirstName));
        Assert.That(entity.LastName, Is.EqualTo(dto.LastName));
        Assert.That(entity.PreferredName, Is.EqualTo(dto.PreferredName));
        Assert.That(entity.CreatedAt, Is.EqualTo(dto.CreatedAt));
        Assert.That(entity.UpdatedAt, Is.EqualTo(dto.UpdatedAt));
    }

    [Test]
    public void FromCreateDto_Trims_And_Sets_Defaults()
    {
        var input = new CreatePersonDto
        {
            FirstName = "  Alice  ",
            LastName = "  Johnson ",
            PreferredName = "  AJ  "
        };

        var before = DateTime.UtcNow.AddSeconds(-1);
        var entity = _factory.FromCreateDto(input);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(entity.Id, Is.EqualTo(0));
        Assert.That(entity.ExternalId, Is.Not.EqualTo(Guid.Empty));
        Assert.Multiple(() =>
        {
            Assert.That(entity.FirstName, Is.EqualTo("Alice"));
            Assert.That(entity.LastName, Is.EqualTo("Johnson"));
            Assert.That(entity.PreferredName, Is.EqualTo("AJ"));
        });
        Assert.That(entity.CreatedAt, Is.InRange(before, after));
        Assert.That(entity.UpdatedAt, Is.InRange(before, after));
    }

    [Test]
    public void UpdateEntityFromDto_Updates_Mutable_Fields_And_Timestamp()
    {
        var entity = new Person
        {
            Id = 1,
            ExternalId = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name",
            PreferredName = null,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var update = new UpdatePersonDto
        {
            FirstName = "  NewFirst  ",
            LastName = "  NewLast  ",
            PreferredName = "  NewPref  "
        };

        var before = DateTime.UtcNow.AddSeconds(-1);
        _factory.UpdateEntityFromDto(entity, update);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.Multiple(() =>
        {
            Assert.That(entity.FirstName, Is.EqualTo("NewFirst"));
            Assert.That(entity.LastName, Is.EqualTo("NewLast"));
            Assert.That(entity.PreferredName, Is.EqualTo("NewPref"));
        });
        Assert.That(entity.UpdatedAt, Is.InRange(before, after));
        // init-only properties must remain unchanged
        Assert.That(entity.CreatedAt, Is.LessThan(entity.UpdatedAt));
    }

    [Test]
    public void UpdateEntityFromDto_Normalizes_Empty_PreferredName_To_Null()
    {
        var entity = new Person
        {
            FirstName = "Old",
            LastName = "Name",
            PreferredName = "OldPref",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var update = new UpdatePersonDto
        {
            FirstName = "NewFirst",
            LastName = "NewLast",
            PreferredName = "   "
        };

        _factory.UpdateEntityFromDto(entity, update);

        Assert.That(entity.PreferredName, Is.Null);
    }
}
