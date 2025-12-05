using Domain.Abstractions;
using Domain.Models;
using Domain.Services;
using Moq;

namespace Tests.Services;

public class IdentityProfileServiceTests
{
    [Test]
    public async Task Preferred_Returns_Default_For_Matching_Language()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = "Work", Language = "en-GB", DisplayName = "A", IsDefaultForContext = true },
            new() { Id = 2, PersonId = 10, Context = "Work", Language = "nb-NO", DisplayName = "B", IsDefaultForContext = false },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = new IdentityProfileService(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, "Work", "en-GB");
        Assert.That(preferred!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Preferred_Falls_Back_To_Any_For_Matching_Language_When_No_Default()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = "Work", Language = "en-GB", DisplayName = "A", IsDefaultForContext = false },
            new() { Id = 2, PersonId = 10, Context = "Work", Language = "nb-NO", DisplayName = "B", IsDefaultForContext = false },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = new IdentityProfileService(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, "Work", "en-GB");
        Assert.That(preferred, Is.Not.Null);
        Assert.That(preferred!.Language, Is.EqualTo("en-GB"));
    }

    [Test]
    public async Task Preferred_Returns_Default_When_No_Language()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = "Work", Language = "en-GB", DisplayName = "A", IsDefaultForContext = true },
            new() { Id = 2, PersonId = 10, Context = "Work", Language = "nb-NO", DisplayName = "B", IsDefaultForContext = false },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = new IdentityProfileService(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, "Work", null);
        Assert.That(preferred!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Preferred_Falls_Back_To_Any_When_No_Default()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = "Work", Language = "en-GB", DisplayName = "A", IsDefaultForContext = false },
            new() { Id = 2, PersonId = 10, Context = "Work", Language = "nb-NO", DisplayName = "B", IsDefaultForContext = false },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = new IdentityProfileService(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, "Work", null);
        Assert.That(preferred, Is.Not.Null);
        Assert.That(preferred!.Context, Is.EqualTo("Work"));
    }

    [Test]
    public async Task Preferred_Returns_Null_When_No_Profiles_In_Context()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = "Social", Language = "en-GB", DisplayName = "A", IsDefaultForContext = false }
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = new IdentityProfileService(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, "Work", null);
        Assert.That(preferred, Is.Null);
    }
}