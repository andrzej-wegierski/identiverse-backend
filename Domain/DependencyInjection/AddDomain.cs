using Domain.Services;
using Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Domain.DependencyInjection;

public static class AddDomainDependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IIdentityProfileService, IdentityProfileService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccessControlService, AccessControlService>();
        
        return services;
    } 
    
}