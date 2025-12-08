using System.Text;
using Domain.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace identiverse_backend.Extensions;

public static class AuthenticationExtension
{
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        
        var jwtSection = configuration.GetSection("Jwt");
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));

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