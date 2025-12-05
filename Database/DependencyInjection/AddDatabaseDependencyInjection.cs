using Database.Factories;
using Database.Repositories;
using Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Database.DependencyInjection;

public static class AddDatabaseDependencyInjection
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.Configure<IdentiverseDatabaseOption>(configuration.GetSection("IdentiverseDatabase"));
        var connectionString = configuration.GetSection("IdentiverseDatabase:ConnectionString").Value;
        services.AddDbContext<IdentiverseDbContext>(options => options.UseNpgsql(connectionString));
        
        // Repositories
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IIdentityProfileRepository, IdentityProfileRepository>();
        
        // Factories
        services.AddScoped<IPersonFactory, PersonFactory>();
        services.AddScoped<IIdentityProfileFactory, IdentityProfileFactory>();

        return services;
    }

    public static void ApplyIdentiverseDatabaseMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentiverseDbContext>();
        db.Database.Migrate();
    }

    internal sealed record IdentiverseDatabaseOption
    {
        public required string ConnectionString { get; init; }
    }
}