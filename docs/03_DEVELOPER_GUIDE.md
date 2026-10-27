# Enterprise Microservice Chassis Developer Guide (.NET 9)

This guide provides end-to-end instructions for engineers developing, extending, testing, and deploying microservices using the **Enterprise .NET 9 Microservice Chassis Template**.

---

## Table of Contents
1. [Architecture Philosophy & System Design](#1-architecture-philosophy--system-design)
2. [Prerequisites & Development Environment](#2-prerequisites--development-environment)
3. [Template Installation & Scaffolding](#3-template-installation--scaffolding)
4. [Solution Anatomy & Strict Layer Boundaries](#4-solution-anatomy--strict-layer-boundaries)
5. [Implementing a New Vertical Slice (Step-by-Step)](#5-implementing-a-new-vertical-slice-step-by-step)
6. [AI Engineering & Vector Search Subsystem](#6-ai-engineering--vector-search-subsystem)
7. [Reliable Messaging & Transactional Outbox](#7-reliable-messaging--transactional-outbox)
8. [Persistence & EF Core Database Migrations](#8-persistence--ef-core-database-migrations)
9. [Observability: Traces, Metrics & Health Probes](#9-observability-traces-metrics--health-probes)
10. [Testing Strategy & Test Execution](#10-testing-strategy--test-execution)
11. [Local Development with Docker Compose](#11-local-development-with-docker-compose)
12. [Production Deployment & Containerization](#12-production-deployment--containerization)

---

## 1. Architecture Philosophy & System Design

The chassis template avoids common microservice pitfalls ("anemic models", "leaky abstractions", "distributed transactions without an outbox", and "disconnected AI prototypes") by enforcing foundational patterns:

```
[ HTTP Minimal APIs ]       [ RabbitMQ Consumer ]
         │                           │
         ▼                           ▼
┌────────────────────────────────────────────────────────┐
│                   Wolverine Mediator                   │
│   (Validation Middleware, Telemetry, Polly Resilience) │
└────────────────────────────┬───────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────┐
│               Vertical Slice Handlers                  │
│       CreateOrderHandler  |  SearchOrdersHandler       │
└──────────────┬─────────────────────────┬───────────────┘
               ▼                         ▼
┌─────────────────────────────┐ ┌────────────────────────┐
│     Domain Aggregates       │ │ Microsoft.Extensions.AI│
│  Order, OrderItem, Money    │ │ Resilient ChatClient   │
└──────────────┬──────────────┘ │ HybridCache Decorator  │
               ▼                └────────────┬───────────┘
┌─────────────────────────────┐              │
│    ApplicationDbContext     │              ▼
│ (EF Core + pgvector + HNSW) │ ┌────────────────────────┐
│   Transactional Outbox      │ │ SemanticSearchService  │
└─────────────────────────────┘ └────────────────────────┘
```

### Core Architecture Pillars:
- **Clean Architecture & Strict Domain Isolation:** The `Domain` project has **0 dependencies**. No ORM, no ASP.NET, no third-party libraries.
- **Vertical Slice Architecture:** Handlers, Commands/Queries, Validators, and DTOs live together in feature folders (`Orders/CreateOrder/`), preventing cross-feature contamination.
- **Transactional Outbox / Durable Inbox:** Guarantees **at-least-once messaging** between the database transaction and RabbitMQ broker via Wolverine.
- **Co-located Vector Search:** Entity tables and vector embeddings reside in the same PostgreSQL database using `pgvector`, eliminating dual-write synchronization bugs.
- **Unified AI Abstraction:** `Microsoft.Extensions.AI` (`IChatClient`, `IEmbeddingGenerator`) with out-of-the-box offline mock support, SSE streaming, resilient retry pipelines, and `HybridCache`.

---

## 2. Prerequisites & Development Environment

### Required Software
- **Operating System:** Linux (Ubuntu 22.04+ recommended), macOS, or Windows WSL2.
- **.NET SDK:** .NET 9.0 LTS SDK (`dotnet --version` >= `9.0.100` or .NET 10 preview SDK with `<RollForward>Major</RollForward>`).
- **Container Runtime:** Docker Engine 24.0+ & Docker Compose v2.

### Verify Prerequisites
```bash
dotnet --version
docker --version
docker compose version
```

---

## 3. Template Installation & Scaffolding

### 3.1 Install Template Locally
Navigate to the repository root directory and install the template:
```bash
dotnet new install .
```

Verify that the template is installed:
```bash
dotnet new list --tag Microservice
```
You should see:
```
Template Name                            Short Name            Language  Tags
---------------------------------------  --------------------  --------  ----------------------------------------
Enterprise .NET 9 Microservice Chassis   microservice-chassis  [C#]      Microservice/Enterprise/.NET 9/Wolverine
```

### 3.2 Scaffold a New Microservice
Create a new service instance in your target project directory:
```bash
dotnet new microservice-chassis -n LogisticsService
cd LogisticsService
```

### 3.3 Uninstall Template (When updating)
```bash
dotnet new uninstall .
```

---

## 4. Solution Anatomy & Strict Layer Boundaries

```
LogisticsService/
├── src/
│   ├── LogisticsService.Domain/           # 0 dependencies. Pure business rules & entities.
│   │   ├── Common/                        # AggregateRoot, Entity, ValueObject, IDomainEvent
│   │   ├── Exceptions/                    # DomainException, NotFoundException
│   │   └── Orders/                        # Order aggregate, OrderItem, Address, Money
│   │
│   ├── LogisticsService.Contracts/        # Public DTOs and integration events published to brokers.
│   │   └── Events/                        # OrderCreatedIntegrationEvent, OrderPaymentReceived
│   │
│   ├── LogisticsService.Application/      # Vertical Slices, Wolverine Handlers, FluentValidation.
│   │   ├── Common/                        # ValidationMiddleware, IApplicationDbContext, Interfaces
│   │   └── Orders/
│   │       ├── CreateOrder/               # Command, Validator, Handler, Response
│   │       ├── ProcessPayment/            # Consumer Handler for external payment events
│   │       └── SearchOrders/              # Vector query, Handler, Semantic Kernel AI tools
│   │
│   ├── LogisticsService.Infrastructure/   # DB Context, pgvector, Wolverine RabbitMQ, AI Pipeline.
│   │   ├── Ai/                            # OfflineMockChatClient, Decorators, SemanticSearchService
│   │   ├── Messaging/                     # Wolverine outbox/inbox, RabbitMQ topic topology
│   │   ├── Observability/                 # OpenTelemetry traces and GenAI metrics
│   │   └── Persistence/                   # ApplicationDbContext, HNSW Index configurations
│   │
│   └── LogisticsService.Api/              # ASP.NET Core Minimal APIs, OpenAPI, ProblemDetails.
│       ├── Endpoints/                     # OrderEndpoints, AiEndpoints (SSE streaming)
│       ├── Health/                        # /health/live, /health/ready Kubernetes probes
│       ├── Middleware/                    # GlobalExceptionHandler (RFC 7807)
│       └── Program.cs                     # Composition root, Scalar interactive UI
│
├── tests/
│   ├── LogisticsService.UnitTests/         # Fast unit tests for Domain, Application, and AI decorators
│   ├── LogisticsService.ArchitectureTests/# ArchUnitNET automated boundary verification
│   └── LogisticsService.IntegrationTests/ # Full slice HTTP tests via WebApplicationFactory
│
└── docker/
    ├── Dockerfile                         # Ubuntu Chiseled non-root multi-stage image
    ├── docker-compose.yml                 # Local dev stack (pgvector + RabbitMQ + API)
    └── init-db.sql                        # PostgreSQL initialization script
```

### Dependency Rules:
- `Domain` depends on **nothing**.
- `Contracts` depends on **nothing** internal (it can be packaged as a public NuGet).
- `Application` depends **only** on `Domain` and `Contracts`.
- `Infrastructure` depends on `Application`, `Domain`, and `Contracts`.
- `Api` depends on `Infrastructure` and `Application`.

---

## 5. Implementing a New Vertical Slice (Step-by-Step)

To illustrate how to add a new business capability, let's implement the **`CancelOrder`** slice.

### Step 1: Add Domain Logic
Add the cancellation behavior and domain event in `Domain`:

```csharp
// src/LogisticsService.Domain/Orders/Events/OrderCancelledDomainEvent.cs
namespace LogisticsService.Domain.Orders.Events;

public sealed record OrderCancelledDomainEvent(Guid OrderId, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
```

Update `Order.cs`:
```csharp
public void Cancel(string reason)
{
    if (Status == OrderStatus.Completed)
        throw new DomainException("Completed orders cannot be cancelled.");

    Status = OrderStatus.Cancelled;
    UpdatedAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new OrderCancelledDomainEvent(Id, reason, UpdatedAt.Value));
}
```

### Step 2: Define Integration Event (Optional)
If downstream consumers need to know about cancellations, define an event in `Contracts`:

```csharp
// src/LogisticsService.Contracts/Events/OrderCancelledIntegrationEvent.cs
namespace LogisticsService.Contracts.Events;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string Reason,
    DateTimeOffset CancelledAt) : IntegrationEvent(OrderId);
```

### Step 3: Implement Application Vertical Slice
Create directory `src/LogisticsService.Application/Orders/CancelOrder/`:

**1. Command (`CancelOrderCommand.cs`):**
```csharp
namespace LogisticsService.Application.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId, string Reason);
```

**2. Validator (`CancelOrderValidator.cs`):**
```csharp
using FluentValidation;

namespace LogisticsService.Application.Orders.CancelOrder;

public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
```

**3. Handler (`CancelOrderHandler.cs`):**
```csharp
using LogisticsService.Application.Common.Interfaces;
using LogisticsService.Contracts.Events;
using LogisticsService.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace LogisticsService.Application.Orders.CancelOrder;

public class CancelOrderHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMessageBus _bus;

    public CancelOrderHandler(IApplicationDbContext dbContext, IMessageBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null)
            throw new OrderNotFoundException(command.OrderId);

        order.Cancel(command.Reason);

        // Transactional Outbox publish
        await _bus.PublishAsync(new OrderCancelledIntegrationEvent(order.Id, command.Reason, DateTimeOffset.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### Step 4: Expose Minimal API Endpoint
Add the endpoint in `src/LogisticsService.Api/Endpoints/OrderEndpoints.cs`:

```csharp
group.MapPost("/{id:guid}/cancel", async (
    Guid id,
    [FromBody] CancelOrderRequest request,
    [FromServices] IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    await bus.InvokeAsync(new CancelOrderCommand(id, request.Reason), cancellationToken);
    return Results.NoContent();
})
.WithName("CancelOrder")
.WithSummary("Cancels an existing order")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesValidationProblem();
```

---

## 6. AI Engineering & Vector Search Subsystem

### 6.1 The AI Pipeline & Decorators
The chassis uses `Microsoft.Extensions.AI` configured as a resilient, decorated pipeline:

```
IChatClient Request
  ──> CachingChatClient (HybridCache L1/L2)
    ──> ResilientChatClient (Polly v8 Retry, Timeout, CircuitBreaker)
      ──> TelemetryChatClient (OpenTelemetry GenAI Activity & Metrics)
        ──> Provider (OfflineMockChatClient | Ollama | OpenAI | Azure | Gemini)
```

### 6.2 Offline Mock Mode (Zero-Cost Development)
In `appsettings.json`:
```json
"Ai": {
  "Provider": "Mock",
  "ModelId": "offline-mock",
  "EmbeddingModelId": "offline-embedding",
  "VectorDimensions": 1536,
  "EnableCaching": true,
  "CacheDurationMinutes": 5,
  "TimeoutSeconds": 15
}
```
- **Simulated Reasoning:** `OfflineMockChatClient` generates deterministic, context-aware markdown syntheses.
- **Deterministic Embeddings:** `OfflineMockEmbeddingGenerator` maps input text to deterministic, L2-normalized 1536-dimensional float vectors using SHA-256 seed hashing. No network calls or cloud API keys required!

### 6.3 Switching to Production AI Providers

#### A. Local AI with Ollama on Ubuntu Linux
To use local Ollama (`llama3.2` or `mistral`):
1. Start Ollama locally: `ollama run llama3.2`
2. Update `appsettings.json`:
```json
"Ai": {
  "Provider": "Ollama",
  "Endpoint": "http://localhost:11434/v1",
  "ModelId": "llama3.2",
  "EmbeddingModelId": "nomic-embed-text"
}
```

#### B. Cloud AI (OpenAI / Azure / Gemini)
Set environment variables:
```bash
export Ai__Provider="OpenAI"
export Ai__ApiKey="sk-proj-xxxxxxxx"
export Ai__ModelId="gpt-4o-mini"
export Ai__EmbeddingModelId="text-embedding-3-small"
```

### 6.4 Co-located pgvector Semantic Search
Vector embeddings are indexed in the `order_embeddings` table using PostgreSQL HNSW:

```csharp
// Entity Framework Core Model Configuration
builder.Property(e => e.Embedding)
    .HasColumnType("vector(1536)")
    .IsRequired();

builder.HasIndex(e => e.Embedding)
    .HasMethod("hnsw")
    .HasOperators("vector_cosine_ops")
    .HasStorageParameter("m", 16)
    .HasStorageParameter("ef_construction", 64);
```

#### Querying with Cosine Distance:
```csharp
var matches = await _dbContext.OrderEmbeddings
    .AsNoTracking()
    .OrderBy(e => e.Embedding.CosineDistance(queryVector))
    .Take(topCount)
    .ToListAsync(cancellationToken);
```

---

## 7. Reliable Messaging & Transactional Outbox

### 7.1 Transactional Outbox Pattern
When `bus.PublishAsync(integrationEvent)` is invoked inside a Wolverine handler, the message is **not** immediately sent across the network to RabbitMQ. Instead, Wolverine enlists the message in the current EF Core database transaction, writing to the `wolverine_messages` table. When `_dbContext.SaveChangesAsync()` commits:
1. Business data and outbox messages commit **atomically**.
2. Wolverine's background agent reads the pending messages and asynchronously pushes them to RabbitMQ.
3. Even if the service crashes after committing the database, Wolverine delivers the outbox message upon recovery.

### 7.2 Durable Inbox & Circuit Breakers
In `WolverineConfiguration.cs`:
```csharp
opts.ListenToRabbitQueue("orders.payment-received")
    .UseDurableInbox()
    .CircuitBreaker(cb =>
    {
        cb.FailurePercentageThreshold = 30; // 30% failure trips circuit
        cb.TrackingPeriod = 1.Minutes();
        cb.PauseTime = 30.Seconds();
    });
```

### 7.3 Poison Message & Dead Letter Queue (DLQ)
```csharp
opts.Policies.OnAnyException()
    .RetryWithCooldown(100.Milliseconds(), 500.Milliseconds(), 2.Seconds())
    .Then.MoveToErrorQueue();
```
Failed messages after exhausted retries are moved to the RabbitMQ Dead Letter Queue (`wolverine.dead-letter-queue`) for manual triage.

---

## 8. Persistence & EF Core Database Migrations

### 8.1 Adding a Migration
When modifying entities in `Domain` or configurations in `Infrastructure`:
```bash
dotnet ef migrations add AddOrderCancellation \
    --project src/ServiceTemplate.Infrastructure \
    --startup-project src/ServiceTemplate.Api
```

### 8.2 Applying Migrations Locally
```bash
dotnet ef database update \
    --project src/ServiceTemplate.Infrastructure \
    --startup-project src/ServiceTemplate.Api
```

---

## 9. Observability: Traces, Metrics & Health Probes

### 9.1 OpenTelemetry Spans & Metrics
The chassis exports OpenTelemetry telemetry via OTLP (`http://localhost:4317`):
- **Traces:** ASP.NET Core requests, EF Core SQL queries, Wolverine message dispatch, and Polly resilience retries.
- **GenAI Spans:** Token counts, model IDs, temperature, and semantic search similarity latency (`gen_ai.client.operation.duration`).

### 9.2 Kubernetes Probes
- **Liveness Probe:** `GET /health/live` returns HTTP 200 `{"status":"Healthy"}` indicating the runtime is responsive.
- **Readiness Probe:** `GET /health/ready` validates PostgreSQL and RabbitMQ connectivity before routing traffic.

---

## 10. Testing Strategy & Test Execution

The template provides three complementary testing tiers with 100% pass rates:

```
┌────────────────────────────────────────────────────────┐
│     tests/ServiceTemplate.ArchitectureTests (ArchUnit) │  Boundary & dependency isolation
├────────────────────────────────────────────────────────┤
│     tests/ServiceTemplate.IntegrationTests (Alba/WAF)  │  Full HTTP slice verification
├────────────────────────────────────────────────────────┤
│     tests/ServiceTemplate.UnitTests (xUnit/Fluent)     │  Fast domain, AI & handler logic
└────────────────────────────────────────────────────────┘
```

### 10.1 Run All Tests
```bash
dotnet test ServiceTemplate.sln
```

### 10.2 Run Architecture Tests
Verifies that Domain has 0 dependencies and Application never references Infrastructure:
```bash
dotnet test tests/ServiceTemplate.ArchitectureTests
```

### 10.3 Run Unit Tests
```bash
dotnet test tests/ServiceTemplate.UnitTests
```

### 10.4 Run Integration Tests
Integration tests run in-memory using `CustomWebApplicationFactory` by default, or with ephemeral Testcontainers when Docker is active:
```bash
dotnet test tests/ServiceTemplate.IntegrationTests
```

---

## 11. Local Development with Docker Compose

### 11.1 Start Infrastructure Dependencies
Spin up PostgreSQL 16 (with `pgvector`) and RabbitMQ 3 Management:
```bash
docker compose -f docker/docker-compose.yml up -d postgres rabbitmq
```

### 11.2 Run the API Service
Run with hot reload:
```bash
dotnet watch --project src/ServiceTemplate.Api
```

### 11.3 Access Dev Tools & Port Map
| Tool / Service | URL | Credentials |
| :--- | :--- | :--- |
| **Scalar Interactive API GUI** | `http://localhost:5000/scalar/v1` | None |
| **OpenAPI 3.0 Document** | `http://localhost:5000/openapi/v1.json` | None |
| **RabbitMQ Management UI** | `http://localhost:15672` | `guest` / `guest` |
| **PostgreSQL Database** | `localhost:5432` | `postgres` / `postgres` (DB: `orders_db`) |
| **Liveness Health Check** | `http://localhost:5000/health/live` | None |
| **Readiness Health Check** | `http://localhost:5000/health/ready` | None |

---

## 12. Production Deployment & Containerization

### 12.1 Build Production Docker Image
The `docker/Dockerfile` utilizes **Ubuntu Chiseled** `.NET 9` runtime images. It creates a hardened, non-root user (`appuser` with UID 10001) and includes zero package managers or shell binaries to minimize attack surface:

```bash
docker build -f docker/Dockerfile -t my-enterprise-service:1.0.0 .
```

### 12.2 Run Production Container
```bash
docker run -d \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__Database="Host=postgres-prod;Port=5432;Database=orders_db;Username=app;Password=secret" \
  -e RabbitMQ__Host="rabbitmq-prod" \
  -e RabbitMQ__Port=5672 \
  -e Wolverine__PersistMessagesWithPostgresql=true \
  -e Wolverine__UseRabbitMq=true \
  -e Ai__Provider="Mock" \
  --name my-service \
  my-enterprise-service:1.0.0
```

### 12.3 Kubernetes Deployment Example
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: order-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: order-service
  template:
    metadata:
      labels:
        app: order-service
    spec:
      containers:
      - name: api
        image: my-enterprise-service:1.0.0
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
        resources:
          limits:
            cpu: "1000m"
            memory: "512Mi"
          requests:
            cpu: "100m"
            memory: "128Mi"
```
