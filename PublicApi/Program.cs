using Database.DependencyInjection;
using Domain.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDomain();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.ApplyIdentiverseDatabaseMigrations();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("swagger/v1/swagger.json", "Identiverse Backend");
        options.RoutePrefix = string.Empty;
    }); 
    app.UseSwagger();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
