using System.Text;
using Database;
using Database.Entities;
using Domain.Models;
using Domain.Security;
using identiverse_backend.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace identiverse_backend.Extensions;

public static class AuthenticationExtension
{
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 10;
            
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<int>>()
        .AddSignInManager()
        .AddEntityFrameworkStores<IdentiverseDbContext>()
        .AddDefaultTokenProviders();
        
        var jwtSection = configuration.GetSection("Jwt");
        var keyBytes = JwtKeyParser.GetSigningKeyBytes(jwtSection["SigningKey"]!);
        
        if (keyBytes.Length == 0)
            throw new InvalidOperationException("JWT SigningKey is missing or empty in configuration. Please provide a valid key in 'Jwt:SigningKey'.");
        
        if (keyBytes.Length < 32)
            Console.WriteLine("WARNING: JWT SigningKey is shorter than 32 bytes. This is not recommended for HMAC-SHA256.");
        
        var signingKey = new SymmetricSecurityKey(keyBytes);
        
        services.AddScoped<IAuthorizationHandler, SelfOrAdminHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromSeconds(30)
                });
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("SelfOrAdmin", policy => policy.AddRequirements(new SelfOrAdminRequirement()));
        });
        
        return services;
    }

    public static IApplicationBuilder UseAuthenticationAndAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        
        return app;
    }
}