using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using identiverse_backend.Controllers;

namespace Tests.Controllers;

public class IdentityProfilesControllerTests
{
    private readonly Mock<IIdentityProfileService> _service = new();

    private IdentityProfilesController CreateSut() => new(_service.Object);

    [Test]
    public async Task GetProfilesForPerson_Returns_Ok_With_List()
    {
        var list = new List<IdentityProfileDto> { new() { Id = 1, PersonId = 10 } };
        _service.Setup(s => s.GetProfilesByPersonAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var controller = CreateSut();
        var action = await controller.GetProfilesForPerson(10, default);

        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(list));
    }

    [Test]
    public async Task GetProfileById_Returns_Ok_When_Found()
    {
        var dto = new IdentityProfileDto { Id = 5, PersonId = 10 };
        _service.Setup(s => s.GetProfileByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = CreateSut();
        var action = await controller.GetProfileById(5, default);

        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task GetProfileById_Returns_NotFound_When_Missing()
    {
        _service.Setup(s => s.GetProfileByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityProfileDto?)null);

        var controller = CreateSut();
        var action = await controller.GetProfileById(999, default);
        Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateProfile_Returns_CreatedAt_With_Payload()
    {
        var create = new CreateIdentityProfileDto { DisplayName = "Alice (Work)", Context = IdentityContext.Legal, Language = "en-GB", IsDefaultForContext = true };
        var created = new IdentityProfileDto { Id = 42, PersonId = 10 };
        _service.Setup(s => s.CreateProfileAsync(10, create, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var controller = CreateSut();
        var action = await controller.CreateProfile(10, create, default);

        Assert.That(action.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)action.Result!;
        Assert.That(result.ActionName, Is.EqualTo(nameof(IdentityProfilesController.GetProfileById)));
        Assert.That(result.RouteValues!["id"], Is.EqualTo(created.Id));
        Assert.That(result.Value, Is.SameAs(created));
    }

    [Test]
    public async Task UpdateProfile_Returns_Ok_When_Updated()
    {
        var update = new UpdateIdentityProfileDto { DisplayName = "Alice (Work)", Context = IdentityContext.Legal, Language = "en-GB", IsDefaultForContext = false };
        var existing = new IdentityProfileDto { Id = 7, PersonId = 10 };
        var updated = new IdentityProfileDto { Id = 7, PersonId = 10, DisplayName = "Updated" };

        _service.Setup(s => s.GetProfileByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _service.Setup(s => s.UpdateProfileAsync(7, update, It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var controller = CreateSut();
        var action = await controller.UpdateProfile(10, 7, update, default);

        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task UpdateProfile_Returns_NotFound_When_Missing()
    {
        var update = new UpdateIdentityProfileDto { DisplayName = "X", Context = IdentityContext.Legal };
        _service.Setup(s => s.GetProfileByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IdentityProfileDto?)null);

        var controller = CreateSut();
        var action = await controller.UpdateProfile(10, 1, update, default);
        Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteProfile_Returns_NoContent_When_Deleted()
    {
        _service.Setup(s => s.GetProfileByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new IdentityProfileDto { Id = 1, PersonId = 10 });
        _service.Setup(s => s.DeleteProfileAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = CreateSut();
        var action = await controller.DeleteProfile(10, 1, default);
        Assert.That(action, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteProfile_Returns_NotFound_When_Missing()
    {
        _service.Setup(s => s.DeleteProfileAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _service.Setup(s => s.GetProfileByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new IdentityProfileDto { Id = 2, PersonId = 10 });

        var controller = CreateSut();
        var action = await controller.DeleteProfile(10, 2, default);
        Assert.That(action, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetPreferredProfile_Returns_Ok_When_Found()
    {
        var dto = new IdentityProfileDto { Id = 100, PersonId = 10, Context = IdentityContext.Legal, Language = "en-GB" };
        _service.Setup(s => s.GetPreferredProfileAsync(10, IdentityContext.Legal, "en-GB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = CreateSut();
        var action = await controller.GetPreferredProfile(10, IdentityContext.Legal, "en-GB", default);

        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task GetPreferredProfile_Returns_NotFound_When_None()
    {
        _service.Setup(s => s.GetPreferredProfileAsync(10, IdentityContext.Legal, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityProfileDto?)null);

        var controller = CreateSut();
        var action = await controller.GetPreferredProfile(10, IdentityContext.Legal, null, default);
        Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());
    }

    // Controllers are thin; access is enforced in domain services, so we only verify pass-through behavior.
}