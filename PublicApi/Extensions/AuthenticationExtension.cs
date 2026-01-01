using System.Text;
using Domain.Models;
using Domain.Security;
using identiverse_backend.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace identiverse_backend.Extensions;

public static class AuthenticationExtension
{
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<PasswordOptions>(configuration.GetSection("Password"));
        
        var jwtSection = configuration.GetSection("Jwt");
        var keyBytes = JwtKeyParser.GetSigningKeyBytes(jwtSection["SigningKey"]!);
        
        if (keyBytes.Length < 32)
        {
            Console.WriteLine("WARNING: JWT SigningKey is shorter than 32 bytes. This is not recommended for HMAC-SHA256.");
        }
        
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