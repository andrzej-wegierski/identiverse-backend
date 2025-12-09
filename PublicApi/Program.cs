using Database.DependencyInjection;
using Domain.DependencyInjection;
using identiverse_backend.Extensions;
using identiverse_backend.Middleware;
using identiverse_backend.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDomain();

builder.Services.AddAuthenticationAndAuthorization(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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

app.UseAuthenticationAndAuthorization();

app.MapControllers();

app.Run();
