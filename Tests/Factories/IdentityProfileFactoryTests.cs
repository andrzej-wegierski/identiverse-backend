using Database.Entities;
using Database.Factories;
using Domain.Enums;
using Domain.Models;

namespace Tests.Factories;

public class IdentityProfileFactoryTests
{
    private readonly IdentityProfileFactory _factory = new();

    [Test]
    public void ToDto_Maps_All_Fields()
    {
        var now = DateTime.UtcNow;
        var entity = new IdentityProfile
        {
            Id = 1,
            PersonId = 10,
            DisplayName = "Work Profile",
            Context = IdentityContext.Legal,
            BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Title = "Software Engineer",
            Email = "work@example.com",
            Phone = "123-456-789",
            Address = "123 Tech St",
            IsDefaultForContext = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var dto = _factory.ToDto(entity);

        Assert.That(dto.Id, Is.EqualTo(entity.Id));
        Assert.That(dto.PersonId, Is.EqualTo(entity.PersonId));
        Assert.That(dto.DisplayName, Is.EqualTo(entity.DisplayName));
        Assert.That(dto.Context, Is.EqualTo(entity.Context));
        Assert.That(dto.BirthDate, Is.EqualTo(entity.BirthDate));
        Assert.That(dto.Title, Is.EqualTo(entity.Title));
        Assert.That(dto.Email, Is.EqualTo(entity.Email));
        Assert.That(dto.Phone, Is.EqualTo(entity.Phone));
        Assert.That(dto.Address, Is.EqualTo(entity.Address));
        Assert.That(dto.IsDefaultForContext, Is.EqualTo(entity.IsDefaultForContext));
        Assert.That(dto.CreatedAt, Is.EqualTo(entity.CreatedAt));
        Assert.That(dto.UpdatedAt, Is.EqualTo(entity.UpdatedAt));
    }

    [Test]
    public void FromCreate_Maps_All_Fields_And_Trims()
    {
        var dto = new CreateIdentityProfileDto
        {
            DisplayName = "  Home Profile  ",
            Context = IdentityContext.Social,
            BirthDate = new DateTime(1995, 5, 5, 0, 0, 0, DateTimeKind.Utc),
            Title = "  Musician  ",
            Email = "  home@example.com  ",
            Phone = "  987-654-321  ",
            Address = "  456 Home Ave  ",
            IsDefaultForContext = false
        };
        int personId = 20;

        var before = DateTime.UtcNow.AddSeconds(-1);
        var entity = _factory.FromCreate(personId, dto);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(entity.PersonId, Is.EqualTo(personId));
        Assert.That(entity.DisplayName, Is.EqualTo("Home Profile"));
        Assert.That(entity.Context, Is.EqualTo(dto.Context));
        Assert.That(entity.BirthDate, Is.EqualTo(dto.BirthDate));
        Assert.That(entity.Title, Is.EqualTo("Musician"));
        Assert.That(entity.Email, Is.EqualTo("home@example.com"));
        Assert.That(entity.Phone, Is.EqualTo("987-654-321"));
        Assert.That(entity.Address, Is.EqualTo("456 Home Ave"));
        Assert.That(entity.IsDefaultForContext, Is.EqualTo(dto.IsDefaultForContext));
        Assert.That(entity.CreatedAt, Is.InRange(before, after));
        Assert.That(entity.UpdatedAt, Is.InRange(before, after));
    }

    [Test]
    public void UpdateEntity_Updates_Mutable_Fields_And_Trims()
    {
        var entity = new IdentityProfile
        {
            DisplayName = "Old Name",
            Context = IdentityContext.Legal,
            BirthDate = new DateTime(1980, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            Title = "Old Title",
            Email = "old@example.com",
            Phone = "000",
            Address = "Old Address",
            IsDefaultForContext = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updateDto = new UpdateIdentityProfileDto
        {
            DisplayName = "  New Name  ",
            Context = IdentityContext.Social,
            BirthDate = new DateTime(1985, 12, 12, 0, 0, 0, DateTimeKind.Utc),
            Title = "  New Title  ",
            Email = "  new@example.com  ",
            Phone = "  111  ",
            Address = "  New Address  ",
            IsDefaultForContext = false
        };

        var before = DateTime.UtcNow.AddSeconds(-1);
        _factory.UpdateEntity(entity, updateDto);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(entity.DisplayName, Is.EqualTo("New Name"));
        Assert.That(entity.Context, Is.EqualTo(updateDto.Context));
        Assert.That(entity.BirthDate, Is.EqualTo(updateDto.BirthDate));
        Assert.That(entity.Title, Is.EqualTo("New Title"));
        Assert.That(entity.Email, Is.EqualTo("new@example.com"));
        Assert.That(entity.Phone, Is.EqualTo("111"));
        Assert.That(entity.Address, Is.EqualTo("New Address"));
        Assert.That(entity.IsDefaultForContext, Is.EqualTo(updateDto.IsDefaultForContext));
        Assert.That(entity.UpdatedAt, Is.InRange(before, after));
    }
}
