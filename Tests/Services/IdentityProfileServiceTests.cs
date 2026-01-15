using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

public class IdentityProfileServiceTests
{
    private readonly Mock<IAccessControlService> _access = new();
    private readonly Mock<ILogger<IdentityProfileService>> _logger = new();
    private IdentityProfileService CreateSut(IIdentityProfileRepository repo)
    {
        _access.Setup(a => a.CanAccessPersonAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _access.Setup(a => a.EnsureCanAccessProfileAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new IdentityProfileService(repo, _access.Object, _logger.Object);
    }

    [Test]
    public async Task Preferred_Returns_Default_When_Exists()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "A", IsDefaultForContext = true },
            new() { Id = 2, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "B", IsDefaultForContext = false },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = CreateSut(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, IdentityContext.Legal);
        Assert.That(preferred!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Preferred_Returns_Default_With_Lowest_Id_When_Multiple_Defaults_Exist()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 10, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "Z", IsDefaultForContext = true },
            new() { Id = 5, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "A", IsDefaultForContext = true },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = CreateSut(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, IdentityContext.Legal);
        Assert.That(preferred!.Id, Is.EqualTo(5));
    }

    [Test]
    public async Task Preferred_Falls_Back_To_Deterministic_Ordering_When_No_Default()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 2, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "B", IsDefaultForContext = false },
            new() { Id = 1, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "A", IsDefaultForContext = false },
            new() { Id = 3, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "B", IsDefaultForContext = false },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = CreateSut(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, IdentityContext.Legal);
        Assert.That(preferred, Is.Not.Null);
        // Should pick DisplayName "A" (Id 1)
        Assert.That(preferred!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Preferred_Falls_Back_To_Id_Ordering_When_Names_Are_Same()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 3, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "A", IsDefaultForContext = false },
            new() { Id = 2, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "A", IsDefaultForContext = false },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = CreateSut(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, IdentityContext.Legal);
        Assert.That(preferred, Is.Not.Null);
        // Should pick DisplayName "A" and lower Id (2)
        Assert.That(preferred!.Id, Is.EqualTo(2));
    }

    [Test]
    public async Task Preferred_Returns_Null_When_No_Profiles_In_Context()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = IdentityContext.Social, DisplayName = "A", IsDefaultForContext = false }
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = CreateSut(repo.Object);

        var preferred = await service.GetPreferredProfileAsync(10, IdentityContext.Legal);
        Assert.That(preferred, Is.Null);
    }

    [Test]
    public async Task Preferred_Returns_Correct_Profile_When_Multiple_Contexts_Exist()
    {
        var profiles = new List<IdentityProfileDto>
        {
            new() { Id = 1, PersonId = 10, Context = IdentityContext.Legal, DisplayName = "Legal 1", IsDefaultForContext = false },
            new() { Id = 2, PersonId = 10, Context = IdentityContext.Social, DisplayName = "Social 1", IsDefaultForContext = true },
        };
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        var service = CreateSut(repo.Object);

        var preferredLegal = await service.GetPreferredProfileAsync(10, IdentityContext.Legal);
        var preferredSocial = await service.GetPreferredProfileAsync(10, IdentityContext.Social);

        Assert.That(preferredLegal!.Id, Is.EqualTo(1));
        Assert.That(preferredSocial!.Id, Is.EqualTo(2));
    }

    [Test]
    public async Task SetDefaultProfile_Returns_True_On_Success()
    {
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfileByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityProfileDto { Id = 1, PersonId = 10 });
        repo.Setup(r => r.SetAsDefaultAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateSut(repo.Object);

        var result = await service.SetDefaultProfileAsync(10, 1);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SetDefaultProfile_Returns_False_If_Profile_NotFound()
    {
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfileByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityProfileDto?)null);
        var service = CreateSut(repo.Object);

        var result = await service.SetDefaultProfileAsync(10, 1);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task SetDefaultProfile_Returns_False_If_Profile_Does_Not_Belong_To_Person()
    {
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfileByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityProfileDto { Id = 1, PersonId = 20 });
        var service = CreateSut(repo.Object);

        var result = await service.SetDefaultProfileAsync(10, 1);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UnsetDefaultProfile_Returns_True_On_Success()
    {
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfileByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityProfileDto { Id = 1, PersonId = 10 });
        repo.Setup(r => r.UnsetDefaultAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateSut(repo.Object);

        var result = await service.UnsetDefaultProfileAsync(10, 1);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UnsetDefaultProfile_Returns_False_If_Profile_NotFound()
    {
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfileByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityProfileDto?)null);
        var service = CreateSut(repo.Object);

        var result = await service.UnsetDefaultProfileAsync(10, 1);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UnsetDefaultProfile_Returns_False_If_Profile_Does_Not_Belong_To_Person()
    {
        var repo = new Mock<IIdentityProfileRepository>();
        repo.Setup(r => r.GetProfileByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityProfileDto { Id = 1, PersonId = 20 });
        var service = CreateSut(repo.Object);

        var result = await service.UnsetDefaultProfileAsync(10, 1);
        Assert.That(result, Is.False);
    }
}
