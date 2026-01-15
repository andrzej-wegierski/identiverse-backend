using Domain.Abstractions;
using identiverse_backend.Services;
using PublicApi.Services;
using Resend;

namespace identiverse_backend.DepdendencyInjection;

public static class AddPublicApiDependencyInjection
{
    public static IServiceCollection AddPublicApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentUserContext, CurrentUserService>();
        services.AddSingleton<ILoginThrottle, InMemoryLoginThrottle>();
        services.AddSingleton<IEmailThrottle, InMemoryEmailThrottle>();
        services.AddScoped<IEmailSender, EmailSender>();

        var resendApiKey = configuration["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend API Key is missing");
        services.AddSingleton<IResend>( sp => ResendClient.Create( resendApiKey ) );
        
        return services;
    }
}