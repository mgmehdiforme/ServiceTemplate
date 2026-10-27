using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ServiceTemplate.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"orders_test_db_{Guid.NewGuid()}";

    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Testing", "true");
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("Wolverine__PersistMessagesWithPostgresql", "false");
        Environment.SetEnvironmentVariable("Wolverine__UseRabbitMq", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                ["Testing"] = "true",
                ["UseInMemoryDatabase"] = "true",
                ["DatabaseName"] = _dbName,
                ["Wolverine:PersistMessagesWithPostgresql"] = "false",
                ["Wolverine:UseRabbitMq"] = "false"
            });
        });
    }
}
