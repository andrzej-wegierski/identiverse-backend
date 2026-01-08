using Domain.Abstractions;
using identiverse_backend.Services;

namespace identiverse_backend.DepdendencyInjection;

public static class AddPublicApiDependencyInjection
{
    public static IServiceCollection AddPublicApi(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentUserContext, CurrentUserService>();
        services.AddSingleton<ILoginThrottle, InMemoryLoginThrottle>();
        services.AddScoped<IEmailSender, LogEmailSender>();
        
        return services;
    }
}