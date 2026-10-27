using Scalar.AspNetCore;
using ServiceTemplate.Api.Endpoints;
using ServiceTemplate.Api.Health;
using ServiceTemplate.Api.Middleware;
using ServiceTemplate.Application;
using ServiceTemplate.Infrastructure;
using ServiceTemplate.Infrastructure.Messaging;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// Configure Wolverine messaging with transactional outbox and RabbitMQ transport
builder.Host.UseWolverine(opts =>
{
    opts.ConfigureWolverine(builder.Configuration);
});

// Register Application vertical slices, validators and Semantic Kernel tools
builder.Services.AddApplicationServices();

// Register Infrastructure (EF Core with pgvector, resilient AI pipeline, OpenTelemetry)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Global Exception Handling (RFC 7807 ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register OpenAPI Document Generation and Health Checks
builder.Services.AddOpenApi();
builder.Services.AddServiceHealthChecks(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

// Map OpenAPI & Scalar interactive documentation GUI
app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Enterprise Microservice API");
        options.WithTheme(ScalarTheme.Moon);
    });
}

// Map Application Endpoint Groups
app.MapOrderEndpoints();
app.MapAiEndpoints();

// Map Kubernetes Liveness & Readiness Probes
app.MapServiceHealthChecks();

app.Run();

// Partial Program class for WebApplicationFactory in integration tests
public partial class Program { }
