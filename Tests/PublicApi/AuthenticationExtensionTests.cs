using identiverse_backend.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.PublicApi;

public class AuthenticationExtensionTests
{
    [Test]
    public void AddAuthenticationAndAuthorization_Throws_When_SigningKey_Is_Empty()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test"
            })
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            services.AddAuthenticationAndAuthorization(configuration));
        
        Assert.That(ex.Message, Does.Contain("JWT SigningKey is missing or empty"));
    }
}