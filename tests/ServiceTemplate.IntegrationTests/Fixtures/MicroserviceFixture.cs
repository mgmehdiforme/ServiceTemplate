using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace ServiceTemplate.IntegrationTests.Fixtures;

/// <summary>
/// Testcontainers fixture spinning up ephemeral PostgreSQL (pgvector) and RabbitMQ instances.
/// </summary>
public class MicroserviceFixture : IAsyncLifetime
{
    public PostgreSqlContainer? PostgreSqlContainer { get; private set; }
    public RabbitMqContainer? RabbitMqContainer { get; private set; }

    public async Task InitializeAsync()
    {
        if (!IsDockerAvailable())
        {
            return;
        }

        PostgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("orders_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        RabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        await Task.WhenAll(PostgreSqlContainer.StartAsync(), RabbitMqContainer.StartAsync());
    }

    public async Task DisposeAsync()
    {
        if (PostgreSqlContainer is not null)
        {
            await PostgreSqlContainer.DisposeAsync();
        }

        if (RabbitMqContainer is not null)
        {
            await RabbitMqContainer.DisposeAsync();
        }
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            return File.Exists("/var/run/docker.sock") || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST"));
        }
        catch
        {
            return false;
        }
    }
}
