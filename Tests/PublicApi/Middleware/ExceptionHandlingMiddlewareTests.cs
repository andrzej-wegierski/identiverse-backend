using System.Net;
using System.Text.Json;
using Domain.Exceptions;
using identiverse_backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.PublicApi.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateMiddleware(Func<HttpContext, Task> next)
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        return new ExceptionHandlingMiddleware(new RequestDelegate(next), logger.Object);
    }

    [Test]
    public async Task When_IdentiverseException_Is_Thrown_Returns_ProblemJson_With_Correct_Status()
    {
        var mw = CreateMiddleware(_ => throw new ConflictException("already exists"));
        var ctx = new DefaultHttpContext();
        var stream = new MemoryStream();
        ctx.Response.Body = stream;

        await mw.InvokeAsync(ctx);

        Assert.That(ctx.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Conflict));
        Assert.That(ctx.Response.ContentType, Is.EqualTo("application/problem+json"));
        stream.Position = 0;
        var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("status").GetInt32(), Is.EqualTo((int)HttpStatusCode.Conflict));
        Assert.That(root.GetProperty("title").GetString(), Is.Not.Null);
        Assert.That(root.GetProperty("detail").GetString(), Does.Contain("already exists"));
        Assert.That(root.TryGetProperty("traceId", out _), Is.True);
    }

    [Test]
    public async Task When_Unknown_Exception_Is_Thrown_Returns_500_ProblemJson()
    {
        var mw = CreateMiddleware(_ => throw new Exception("boom"));
        var ctx = new DefaultHttpContext();
        var stream = new MemoryStream();
        ctx.Response.Body = stream;

        await mw.InvokeAsync(ctx);

        Assert.That(ctx.Response.StatusCode, Is.EqualTo(500));
        Assert.That(ctx.Response.ContentType, Is.EqualTo("application/problem+json"));
    }

    [Test]
    public async Task When_No_Exception_Is_Thrown_Passes_Through()
    {
        var mw = CreateMiddleware(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("OK");
        });
        var ctx = new DefaultHttpContext();
        var stream = new MemoryStream();
        ctx.Response.Body = stream;

        await mw.InvokeAsync(ctx);
        Assert.That(ctx.Response.StatusCode, Is.EqualTo(200));
        stream.Position = 0;
        var text = new StreamReader(stream).ReadToEnd();
        Assert.That(text, Is.EqualTo("OK"));
    }
}
