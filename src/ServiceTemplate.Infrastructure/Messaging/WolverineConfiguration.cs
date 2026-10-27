using JasperFx.Core;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ServiceTemplate.Application.Common.Behaviours;
using ServiceTemplate.Application.Orders.CreateOrder;
using ServiceTemplate.Contracts.Events;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace ServiceTemplate.Infrastructure.Messaging;

/// <summary>
/// Configures Wolverine transactional outbox, durable inbox, RabbitMQ transport, and failure handling.
/// </summary>
public static class WolverineConfiguration
{
    public static void ConfigureWolverine(this WolverineOptions opts, IConfiguration configuration)
    {
        // Discover message handlers in the Application assembly
        opts.Discovery.IncludeAssembly(typeof(CreateOrderHandler).Assembly);

        var connectionString = configuration.GetConnectionString("Database")
            ?? "Host=localhost;Port=5432;Database=orders_db;Username=postgres;Password=postgres";

        var isTestEnvironment = string.Equals(configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT"), "Testing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Testing", StringComparison.OrdinalIgnoreCase)
            || configuration.GetValue<bool>("Testing", false);

        var persistWithPostgres = !isTestEnvironment && configuration.GetValue<bool>("Wolverine:PersistMessagesWithPostgresql", false);
        var useRabbitMq = !isTestEnvironment && configuration.GetValue<bool>("Wolverine:UseRabbitMq", false);

        // 1. Transactional Outbox/Inbox with EF Core & PostgreSQL
        if (persistWithPostgres)
        {
            opts.UseEntityFrameworkCoreTransactions();
            opts.Policies.AutoApplyTransactions();
            opts.PersistMessagesWithPostgresql(connectionString, "wolverine_messages");
        }

        // 2. Transport Configuration (RabbitMQ)
        if (useRabbitMq)
        {
            var rabbitConfig = configuration.GetSection("RabbitMQ");
            var rabbitHost = rabbitConfig.GetValue<string>("Host") ?? "localhost";
            var rabbitPort = rabbitConfig.GetValue<int>("Port", 5672);
            var rabbitUser = rabbitConfig.GetValue<string>("Username") ?? "guest";
            var rabbitPass = rabbitConfig.GetValue<string>("Password") ?? "guest";

            opts.UseRabbitMq(new Uri($"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:{rabbitPort}"))
                .AutoProvision()
                .ConfigureSenders(s => s.UseDurableOutbox());

            // Publish public integration events to RabbitMQ topic exchange
            opts.PublishMessage<OrderCreatedIntegrationEvent>()
                .ToRabbitTopic("order.created", "orders.topic");

            // Listen to incoming payment events via durable queue with Inbox
            opts.ListenToRabbitQueue("orders.payment-received")
                .UseDurableInbox()
                .CircuitBreaker(cb =>
                {
                    cb.FailurePercentageThreshold = 30;
                    cb.TrackingPeriod = 1.Minutes();
                    cb.PauseTime = 30.Seconds();
                });
        }

        // 3. Global Failure & Dead Letter Queue (Poison Message) Policies
        opts.Policies.OnException<NpgsqlException>()
            .RetryWithCooldown(50.Milliseconds(), 200.Milliseconds(), 1.Seconds());

        opts.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(100.Milliseconds(), 500.Milliseconds(), 2.Seconds());

        opts.Policies.OnAnyException()
            .RetryWithCooldown(100.Milliseconds(), 500.Milliseconds(), 2.Seconds())
            .Then.MoveToErrorQueue();

        // 4. In-Process Validation Middleware Pipeline
        opts.Policies.AddMiddleware(typeof(ValidationMiddleware));
    }
}
