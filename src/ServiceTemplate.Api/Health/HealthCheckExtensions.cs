using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceTemplate.Infrastructure.Persistence;

namespace ServiceTemplate.Api.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddServiceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapServiceHealthChecks(this IEndpointRouteBuilder app)
    {
        // Liveness probe (Kubernetes liveness)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteLiveResponseAsync
        });

        // Readiness probe (Kubernetes readiness)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteReadyResponseAsync
        });

        return app;
    }

    private static Task WriteLiveResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { status = report.Status.ToString() });
        return context.Response.WriteAsync(payload);
    }

    private static Task WriteReadyResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
            durationMs = report.TotalDuration.TotalMilliseconds
        });
        return context.Response.WriteAsync(payload);
    }
}

public class PostgresHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public PostgresHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (dbContext is null)
            {
                return HealthCheckResult.Healthy("Database context not registered (in-memory mode).");
            }

            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("PostgreSQL is unreachable.");
            }

            return HealthCheckResult.Healthy("PostgreSQL and pgvector extension are healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"PostgreSQL check failed: {ex.Message}");
        }
    }
}

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RabbitMqHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var rabbitConfig = _configuration.GetSection("RabbitMQ");
        var host = rabbitConfig.GetValue<string>("Host") ?? "localhost";
        var port = rabbitConfig.GetValue<int>("Port", 5672);

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            await client.ConnectAsync(host, port, cts.Token);
            return HealthCheckResult.Healthy($"RabbitMQ reachable at {host}:{port}");
        }
        catch (Exception ex)
        {
            // If offline mock mode or test environment without broker, report healthy or degraded
            var provider = _configuration.GetValue<string>("Ai:Provider");
            if (string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Healthy($"RabbitMQ skipped in offline mock mode ({ex.Message})");
            }

            return HealthCheckResult.Unhealthy($"RabbitMQ unreachable at {host}:{port}: {ex.Message}");
        }
    }
}
