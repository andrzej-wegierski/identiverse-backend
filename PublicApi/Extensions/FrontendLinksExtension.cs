using Domain.Models;
using Microsoft.Extensions.Options;

namespace identiverse_backend.Extensions;

public static class FrontendLinksExtension
{
    public static IServiceCollection AddFrontendLinks(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOptions<FrontendLinksOptions>()
            .Bind(configuration.GetSection(FrontendLinksOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "BaseUrl is required")
            .PostConfigure(o =>
            {
                if (string.IsNullOrWhiteSpace(o.BaseUrl) && environment.IsDevelopment())
                {
                    o.BaseUrl = "http://localhost:5173";
                }
            })
            .ValidateOnStart();

        return services;
    }
}
