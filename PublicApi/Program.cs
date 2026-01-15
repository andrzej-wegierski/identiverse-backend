using Database.DependencyInjection;
using Domain.Abstractions;
using Domain.DependencyInjection;
using Domain.Models;
using identiverse_backend.DepdendencyInjection;
using identiverse_backend.Extensions;
using identiverse_backend.Middleware;
using identiverse_backend.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddFrontendCors();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDomain();
builder.Services.AddPublicApi(builder.Configuration);

builder.Services.AddFrontendLinks(builder.Configuration, builder.Environment);

builder.Services.AddAuthenticationAndAuthorization(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddSwagger();

var app = builder.Build();

app.Services.ApplyIdentiverseDatabaseMigrations();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("identiverse-frontend");

app.UseAuthenticationAndAuthorization();

app.MapControllers();

app.Run();
