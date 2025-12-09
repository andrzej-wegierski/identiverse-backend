using System.Security.Claims;
using identiverse_backend.Services;
using Microsoft.AspNetCore.Http;

namespace Tests.PublicApi.Services;

public class CurrentUserServiceTests
{
    private static DefaultHttpContext CreateContext(ClaimsPrincipal? principal)
    {
        var ctx = new DefaultHttpContext();
        if (principal != null) ctx.User = principal;
        return ctx;
    }

    private static ClaimsPrincipal CreatePrincipal(bool authenticated, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticated ? "Test" : null);
        return new ClaimsPrincipal(identity);
    }

    [Test]
    public void IsAuthenticated_Returns_True_For_Authenticated_Principal()
    {
        var principal = CreatePrincipal(true, new Claim(ClaimTypes.Name, "user"));
        var accessor = new HttpContextAccessor { HttpContext = CreateContext(principal) };
        var svc = new CurrentUserService(accessor);
        Assert.That(svc.IsAuthenticated, Is.True);
    }

    [Test]
    public void IsAuthenticated_Returns_False_For_Anonymous()
    {
        var principal = CreatePrincipal(false);
        var accessor = new HttpContextAccessor { HttpContext = CreateContext(principal) };
        var svc = new CurrentUserService(accessor);
        Assert.That(svc.IsAuthenticated, Is.False);
    }

    [Test]
    public void IsAdmin_Returns_True_When_Role_Admin()
    {
        var principal = CreatePrincipal(true, new Claim(ClaimTypes.Role, "Admin"));
        var accessor = new HttpContextAccessor { HttpContext = CreateContext(principal) };
        var svc = new CurrentUserService(accessor);
        Assert.That(svc.IsAdmin, Is.True);
    }

    [Test]
    public void GetCurrentUser_Returns_Null_When_Not_Authenticated()
    {
        var principal = CreatePrincipal(false);
        var accessor = new HttpContextAccessor { HttpContext = CreateContext(principal) };
        var svc = new CurrentUserService(accessor);
        Assert.That(svc.GetCurrentUser(), Is.Null);
    }

    [Test]
    public void GetCurrentUser_Parses_UserId_Username_Role_And_PersonId_Correctly()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "15"),
            new Claim(ClaimTypes.Name, "alice"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim("personId", "99")
        };
        var principal = CreatePrincipal(true, claims);
        var accessor = new HttpContextAccessor { HttpContext = CreateContext(principal) };
        var svc = new CurrentUserService(accessor);
        var cu = svc.GetCurrentUser();
        Assert.That(cu, Is.Not.Null);
        Assert.That(cu!.UserId, Is.EqualTo(15));
        Assert.That(cu.Username, Is.EqualTo("alice"));
        Assert.That(cu.Role, Is.EqualTo("User"));
        Assert.That(cu.PersonId, Is.EqualTo(99));
    }

    [Test]
    public void GetCurrentUser_Parses_PersonId_Ignoring_Case()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "21"),
            new Claim(ClaimTypes.Name, "bob"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim("PersonId", "123") // different casing
        };
        var principal = CreatePrincipal(true, claims);
        var accessor = new HttpContextAccessor { HttpContext = CreateContext(principal) };
        var svc = new CurrentUserService(accessor);
        var cu = svc.GetCurrentUser();
        Assert.That(cu, Is.Not.Null);
        Assert.That(cu!.UserId, Is.EqualTo(21));
        Assert.That(cu.PersonId, Is.EqualTo(123));
    }
}
