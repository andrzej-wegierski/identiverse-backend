using Domain.Services;
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
        services.AddScoped<IAccessControllService, AccessControllService>();
        
        return services;
    } 
    
}