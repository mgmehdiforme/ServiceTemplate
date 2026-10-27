# Product Requirements & Acceptance Specification (PRD)
## Enterprise Microservice Chassis Template (.NET 9 / C# 13)

- **Document Version:** 1.0.0
- **Status:** Approved for Implementation (Ready for Phase 3)
- **Author:** Product Manager (PM)
- **Architectural Reference:** [01_ARCHITECTURE_SPECIFICATION.md](file:///media/Projects/PersonalProjects/Documention/01_ARCHITECTURE_SPECIFICATION.md)
- **Target Platform:** .NET 9.0 LTS (C# 13), Ubuntu Linux (x64 / arm64), Docker / Podman, Kubernetes
- **Distribution Model:** `dotnet new` template (`dotnet new microservice-chassis -n <ServiceName>`)

---

## 1. Executive Summary & Product Vision

### 1.1 Vision Statement
The **Enterprise Microservice Chassis Template** is a developer platform asset that empowers engineering teams to bootstrap cloud-native, AI-augmented, event-driven microservices in **under 30 seconds**. 

Instead of re-inventing distributed messaging, outbox consistency, resilience decorators, observability, and vector database integrations on every project, developers get a production-ready, zero-technical-debt chassis out-of-the-box. The chassis strictly enforces architectural boundaries while eliminating the traditional friction of local development.

### 1.2 Target Personas

| Persona | Role & Context | Primary Needs & Pain Points Addressed |
| :--- | :--- | :--- |
| **Backend / Microservice Developer** | Daily feature implementer working on Ubuntu Linux or WSL2. | **Needs:** Instant setup via `dotnet new`, zero-configuration local test execution, no cloud API key blocker, clear vertical slice patterns.<br>**Pain Point:** Wasting 3–5 days configuring outbox tables, message brokers, and DI boilerplate before writing the first line of business code. |
| **Platform / Enterprise Architect** | Responsible for system scalability, consistency, and compliance across microservices. | **Needs:** Compile-time and CI-verified architecture guardrails (ArchUnitNET), at-least-once messaging guarantees (Transactional Outbox/Inbox), standardized OpenTelemetry GenAI semantics.<br>**Pain Point:** Architectural erosion, dual-write bugs, and disparate AI implementations scattered across microservices. |
| **Operations & SRE** | Manages container deployment, observability, and runtime reliability in Kubernetes. | **Needs:** Hardened, non-root chiseled container images, granular liveness/readiness probes, predictable DLQ poison-message routing.<br>**Pain Point:** Bloated root-running containers, brittle health checks, and unmonitored external AI outages causing cascading failures. |
| **API Consumer / Client Application** | Web frontends, mobile apps, or upstream microservices consuming APIs. | **Needs:** Low-latency transactional endpoints, idempotent asynchronous processing, streaming real-time AI responses (SSE), RFC 7807 structured errors.<br>**Pain Point:** Opaque 500 errors, duplicate charge execution, and slow synchronous AI calls blocking HTTP threads. |

---

## 2. Feature Priority & Scope Matrix (MoSCoW)

```
+---------------------------------------------------------------------------------------------------+
|                                     MoSCoW Feature Scope Matrix                                   |
|                                                                                                   |
|  [MUST HAVE (MVP - Phase 3)]                                                                      |
|  - US-1: dotnet new template scaffolding & zero-dependency offline mock execution.                |
|  - US-2: Minimal API Order Creation with FluentValidation & Wolverine Transactional Outbox.       |
|  - US-3: RabbitMQ Payment Consumer with Wolverine Durable Inbox deduplication & DLQ.              |
|  - US-4: Co-located pgvector Cosine Search & IChatClient synthesis with SSE streaming.           |
|  - US-5: ArchUnitNET boundary tests, Chiseled Ubuntu Dockerfile & deep Health Checks.            |
|                                                                                                   |
|  [SHOULD HAVE (Immediate Next Iteration)]                                                         |
|  - Distributed OpenTelemetry trace export visualization via Aspire / Jaeger dashboard.           |
|  - HybridCache Redis L2 secondary tier integration with in-memory L1 fallback.                   |
|                                                                                                   |
|  [COULD HAVE (Future Backlog)]                                                                    |
|  - Webhook delivery subscription slice with signed HMAC payload verification.                     |
|  - Tenant-isolated multi-tenant schema partitioning option in template parameters.                |
|                                                                                                   |
|  [WON'T HAVE (Out of Scope for Chassis)]                                                          |
|  - External standalone vector databases (e.g. Pinecone/Qdrant) - pgvector is the single choice.   |
|  - Monolithic horizontal layering or MediatR/MassTransit coexistence.                             |
+---------------------------------------------------------------------------------------------------+
```

---

## 3. Detailed User Stories & Acceptance Criteria

```
+---------------------------------------------------------------------------------------------------+
|                                      User Story Map & Flow                                        |
|                                                                                                   |
|   +--------------------------------------------------------------------------------------------+  |
|   | US-1: Developer Scaffolding & Zero-Setup First Run                                         |  |
|   | [dotnet new] ---> [Builds in <10s] ---> [Mock AI Active] ---> [All Unit Tests Pass]        |  |
|   +---------------------------------------------+----------------------------------------------+  |
|                                                 |                                                 |
|   +---------------------------------------------v----------------------------------------------+  |
|   | US-2: Transactional Outbox Order Creation                                                  |  |
|   | [POST /api/orders] ---> [FluentValidation] ---> [Postgres Order + Outbox Envelope (1 Tx)]   |  |
|   +---------------------------------------------+----------------------------------------------+  |
|                                                 |                                                 |
|   +---------------------------------------------v----------------------------------------------+  |
|   | US-3: Idempotent Asynchronous Payment Processing                                           |  |
|   | [RabbitMQ: OrderPaymentReceived] ---> [Durable Inbox Dedupe] ---> [Status: Paid + Vector]   |  |
|   +---------------------------------------------+----------------------------------------------+  |
|                                                 |                                                 |
|   +---------------------------------------------v----------------------------------------------+  |
|   | US-4: Co-located Semantic Search & AI Synthesis                                            |  |
|   | [GET /api/ai/orders/search] ---> [pgvector Cosine Search] ---> [IChatClient Synthesis/SSE] |  |
|   +---------------------------------------------+----------------------------------------------+  |
|                                                 |                                                 |
|   +---------------------------------------------v----------------------------------------------+  |
|   | US-5: Architecture Governance & Quality Gates                                              |  |
|   | [ArchUnitNET Tests] + [Chiseled Dockerfile] + [/health/live & /health/ready]               |  |
|   +--------------------------------------------------------------------------------------------+  |
+---------------------------------------------------------------------------------------------------+
```

---

### US-1: Developer Scaffolding & Zero-Setup First Run

**As a** backend software engineer,  
**I want** to execute `dotnet new microservice-chassis -n <MyServiceName>` and immediately run `dotnet build` and `dotnet test` with offline mock AI enabled,  
**So that** I have a working, compilable, and tested microservice chassis in under 30 seconds without requiring Docker, live database connections, or paid cloud AI API keys.

#### Context & Business Rules:
1. The repository root contains `.template.config/template.json` conforming to the official .NET template engine specifications.
2. The template substitution symbol is `ServiceTemplate`. All folder paths, namespaces, project file names, and references must rename dynamically when instantiated.
3. Default configuration in `appsettings.Development.json` has `AiOptions:Provider` set to `"Mock"`.
4. When `Provider == "Mock"`, `OfflineMockChatClient` and `OfflineMockEmbeddingGenerator` are registered, producing deterministic math-based vector embeddings (1536 float dimensions) and simulated chat responses without internet connectivity.
5. All unit tests (`ServiceTemplate.UnitTests`) execute in-memory and pass out-of-the-box.

#### Acceptance Criteria:

##### Scenario 1.1: Template Instantiation & Namespace Substitution
- **Given** the .NET 9 SDK is installed on an Ubuntu Linux environment
- **When** the developer executes `dotnet new install .` followed by `dotnet new microservice-chassis -n Acme.PaymentService -o ./Acme.PaymentService`
- **Then** the target directory `./Acme.PaymentService` is created containing:
  - `src/Acme.PaymentService.Contracts/Acme.PaymentService.Contracts.csproj`
  - `src/Acme.PaymentService.Domain/Acme.PaymentService.Domain.csproj`
  - `src/Acme.PaymentService.Application/Acme.PaymentService.Application.csproj`
  - `src/Acme.PaymentService.Infrastructure/Acme.PaymentService.Infrastructure.csproj`
  - `src/Acme.PaymentService.Api/Acme.PaymentService.Api.csproj`
  - `tests/Acme.PaymentService.UnitTests/Acme.PaymentService.UnitTests.csproj`
  - `tests/Acme.PaymentService.IntegrationTests/Acme.PaymentService.IntegrationTests.csproj`
  - `tests/Acme.PaymentService.ArchitectureTests/Acme.PaymentService.ArchitectureTests.csproj`
- **And** all C# namespace statements throughout all files are renamed from `ServiceTemplate.*` to `Acme.PaymentService.*`.

##### Scenario 1.2: Clean Zero-Setup Compilation
- **Given** a freshly scaffolded project from `dotnet new microservice-chassis`
- **When** the developer executes `dotnet build --nologo`
- **Then** the build completes with `0 Warning(s)` and `0 Error(s)`
- **And** compiler warnings treated as errors (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`) are fully satisfied.

##### Scenario 1.3: Out-of-the-Box Unit Test Execution
- **Given** a freshly scaffolded project with zero external dependencies running (no Docker, no PostgreSQL, no RabbitMQ)
- **When** the developer executes `dotnet test tests/ServiceTemplate.UnitTests`
- **Then** all domain unit tests and application slice validator tests pass with a 100% success rate
- **And** test execution completes in under 5 seconds.

##### Scenario 1.4: Offline Mock AI Determinism
- **Given** `appsettings.Development.json` configured with:
  ```json
  "Ai": {
    "Provider": "Mock",
    "ModelId": "offline-mock"
  }
  ```
- **When** `IEmbeddingGenerator<string, Embedding<float>>` is invoked with input `"High-end gaming workstation"`
- **Then** the generator returns a normalized vector of length `1536`
- **And** identical input strings produce identical vector values across test runs.
- **When** `IChatClient.CompleteAsync()` is invoked with a prompt
- **Then** a structured, human-readable simulated response is returned within 50 milliseconds without making any outbound HTTP requests.

##### Scenario 1.5: Template Optional Feature Flags
- **Given** the template engine configuration
- **When** a user generates a service with `--includeRabbitMq false`
- **Then** RabbitMQ transport registration is omitted from the generated infrastructure, falling back to Wolverine's in-memory / local outbox transport.

---

### US-2: Transactional Outbox Order Creation

**As an** API consumer / purchasing client,  
**I want** to submit a new order through `POST /api/orders` with strongly-typed JSON,  
**So that** my order is validated, atomically committed to the database along with an `OrderCreatedIntegrationEvent` envelope in the transactional outbox, and acknowledged with a `201 Created` response.

#### Context & Business Rules:
1. Route: `POST /api/orders`.
2. Input payload:
   ```json
   {
     "customerId": "cust_12345",
     "items": [
       { "sku": "PROD-A", "productName": "Ergonomic Mechanical Keyboard", "quantity": 2, "unitPrice": 149.99 }
     ],
     "shippingAddress": {
       "street": "10 Downing St",
       "city": "London",
       "country": "UK"
     }
   }
   ```
3. Validation Rules (FluentValidation via `CreateOrderValidator`):
   - `customerId`: Not empty, max length 100.
   - `items`: Must contain at least 1 item.
   - Each item: `sku` not empty, `quantity` > 0, `unitPrice` > 0.00.
   - `shippingAddress`: `street`, `city`, and `country` must not be empty.
4. Persistence Invariant:
   - EF Core `ApplicationDbContext` writes `Order` and its child `OrderItem`s.
   - Wolverine publishes `OrderCreatedIntegrationEvent` within the exact same database transaction.
   - An envelope record is written into `wolverine_outgoing_envelopes`.
   - The state transition and outbox record commit atomically. If saving fails, no message is ever sent.
5. Response:
   - On success: `201 Created` with `Location: /api/orders/{id}` and body `{ "id": "<guid>", "totalAmount": 299.98, "currency": "USD", "status": "Pending" }`.
   - On validation failure: `400 Bad Request` with `RFC 7807 ProblemDetails` format detailing field errors.

#### Acceptance Criteria:

##### Scenario 2.1: Successful Order Creation with Guaranteed Outbox Persistence
- **Given** a running API instance connected to PostgreSQL
- **When** a client posts a valid order creation payload to `POST /api/orders`
- **Then** the API responds with HTTP status `201 Created`
- **And** the response body contains a valid GUID `id`, status `"Pending"`, and computed total amount
- **And** a row exists in table `orders` with status `"Pending"`
- **And** a corresponding envelope exists in table `wolverine_messages.wolverine_outgoing_envelopes` carrying message type `ServiceTemplate.Contracts.Events.OrderCreatedIntegrationEvent`
- **And** both records share the exact same database transaction commit timestamp.

##### Scenario 2.2: Payload Validation Failure (RFC 7807 ProblemDetails)
- **Given** an invalid payload where `items` is empty and `customerId` is missing
- **When** the client submits the request to `POST /api/orders`
- **Then** the API responds with HTTP status `400 Bad Request`
- **And** the `Content-Type` header is `application/problem+json`
- **And** the JSON body adheres to RFC 7807 containing:
  - `"type"`: `"https://tools.ietf.org/html/rfc7231#section-6.5.1"`
  - `"title"`: `"One or more validation errors occurred."`
  - `"status"`: `400`
  - `"errors"` dictionary explicitly highlighting `"CustomerId"` and `"Items"` failure reasons.
- **And** zero rows are inserted into `orders` or `wolverine_outgoing_envelopes`.

##### Scenario 2.3: Database Transaction Rollback on Unhandled Error
- **Given** a simulated database outage or unhandled exception during handler execution
- **When** the command execution aborts prior to transaction commit
- **Then** the API responds with HTTP status `500 Internal Server Error` (ProblemDetails)
- **And** neither the order entity nor any outbox envelope is persisted to the database.

##### Scenario 2.4: Asynchronous Outbox Relay to RabbitMQ
- **Given** a successfully committed order and outbox envelope
- **When** the Wolverine background agent scans `wolverine_outgoing_envelopes`
- **Then** the message is published to the RabbitMQ exchange `orders.topic` with routing key `order.created`
- **And** upon broker ACK, the envelope record is deleted or marked processed in `wolverine_outgoing_envelopes`.

##### Scenario 2.5: W3C Distributed Tracing Propagation
- **Given** an incoming HTTP request with header `traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01`
- **When** the order is created and the outbox envelope is generated
- **Then** the Wolverine outgoing envelope retains the incoming `traceparent` correlation ID in its metadata headers, preserving end-to-end distributed trace continuity.

---

### US-3: Idempotent Asynchronous Payment Processing

**As an** event-driven messaging subscriber,  
**I want** Wolverine to consume `OrderPaymentReceived` messages from RabbitMQ and process them through a durable inbox,  
**So that** order status transitions to `Paid` and vector embeddings are generated idempotently, discarding duplicate deliveries without dual charges or corrupt state.

#### Context & Business Rules:
1. Message Contract:
   ```csharp
   public record OrderPaymentReceived(
       Guid OrderId,
       string PaymentTransactionId,
       decimal AmountPaid,
       string Currency,
       DateTimeOffset PaidAt
   );
   ```
2. Inbound Queue: `orders.payment-received` bound to topic exchange `orders.topic`.
3. Inbox Deduplication:
   - Wolverine executes with `.UseDurableInbox()`.
   - Incoming envelope ID is checked against `wolverine_incoming_envelopes`.
   - If already recorded, message execution is skipped and acknowledged immediately.
4. Business Logic on First Delivery:
   - Find Order by `OrderId`. If not found, throw `OrderNotFoundException` (logs warning, does not crash).
   - Verify `Order.Status` is `Pending`. Transition to `Paid` using domain method `order.MarkAsPaid()`.
   - Synthesize order textual summary (e.g. `"Order <id> containing 2 Ergonomic Keyboards for customer cust_12345 totaling 299.98 USD"`).
   - Generate 1536-dimensional vector embedding using `IEmbeddingGenerator`.
   - Insert `OrderEmbedding` entity into `order_embeddings` table.
   - Commit transaction; update inbox record status to completed.
5. Concurrency & Retry Invariants:
   - Optimistic concurrency control via PostgreSQL `xmin` column.
   - Transient database errors (`NpgsqlException`, `TimeoutException`) retry with cooldowns: 50ms, 200ms, 1s.
   - Unhandled non-transient failures after 3 attempts are routed to `orders.poison-dlq`.

#### Acceptance Criteria:

##### Scenario 3.1: Normal Happy Path Payment Reception
- **Given** an existing order in database with status `"Pending"`
- **When** an `OrderPaymentReceived` message with a unique `Envelope.Id` arrives from RabbitMQ
- **Then** the message is processed by `OrderPaymentReceivedHandler`
- **And** the order status in table `orders` is updated to `"Paid"`
- **And** a row is inserted into table `order_embeddings` with matching `order_id` and a 1536-element float array
- **And** the envelope ID is recorded as processed in `wolverine_incoming_envelopes`
- **And** the message is acknowledged to RabbitMQ.

##### Scenario 3.2: Duplicate Message Delivery (Idempotent Inbox Deduplication)
- **Given** an `OrderPaymentReceived` message that was already successfully processed in Scenario 3.1
- **When** RabbitMQ redelivers the exact same envelope (same `Envelope.Id`) due to network retransmission or cluster rebalance
- **Then** Wolverine's durable inbox detects the existing envelope ID in `wolverine_incoming_envelopes`
- **And** the `OrderPaymentReceivedHandler` business logic is NOT executed a second time
- **And** zero updates occur to the `orders` or `order_embeddings` tables
- **And** the redelivered message is safely acknowledged and discarded without error.

##### Scenario 3.3: Concurrency Conflict Resolution
- **Given** an order being modified concurrently by another thread
- **When** `OrderPaymentReceivedHandler` encounters a concurrency token conflict (`DbUpdateConcurrencyException`)
- **Then** Wolverine re-loads the latest aggregate state and executes retry logic according to configured resilience policy without data loss.

##### Scenario 3.4: Poison Message Dead-Letter Queue (DLQ) Routing
- **Given** a corrupted message payload causing a non-transient deserialization or domain invariant error
- **When** the message fails execution 3 consecutive times across exponential backoff intervals
- **Then** Wolverine ceases further retries and forwards the dead message envelope into `orders.poison-dlq`
- **And** an error metric is recorded in OpenTelemetry.

---

### US-4: Co-located Semantic Search & AI Synthesis

**As a** customer support agent or business analyst,  
**I want** to search orders using natural language via `GET /api/ai/orders/search?q={query}` (and stream via Server-Sent Events),  
**So that** I receive orders ranked by cosine similarity directly from PostgreSQL (pgvector) along with an AI-generated natural language synthesis without latency spikes.

#### Context & Business Rules:
1. Endpoints:
   - Standard REST: `GET /api/ai/orders/search?q={query}&topK={topK}`
   - Streaming SSE: `GET /api/ai/orders/search/stream?q={query}&topK={topK}`
2. Execution Sequence:
   - Convert `q` into a query vector using `IEmbeddingGenerator<string, Embedding<float>>`.
   - Query `order_embeddings` using EF Core + pgvector:
     ```csharp
     .OrderBy(e => e.Embedding.CosineDistance(queryVector))
     .Take(topK)
     ```
   - Calculate similarity score: `Similarity = 1.0 - CosineDistance`.
   - Send matched items and context to `IChatClient` with prompt:
     `"You are an assistant. Synthesize the customer orders found: [orders] for query: [q]"`
   - Return combined results: matched orders list + AI synthesis.
3. Resilience & Caching Decorators:
   - AI calls pass through `TelemetryChatClient` -> `ResilienceChatClient` -> `CachingChatClient`.
   - Identical queries within 5 minutes return cached synthesis from `HybridCache`.
   - External AI timeouts are capped at 15 seconds. If the live AI provider fails or circuits open, a graceful fallback response is generated without returning HTTP 500 to the client.
4. Streaming SSE Format:
   - `Content-Type: text/event-stream; charset=utf-8`
   - Real-time token emission: `data: {"token": "..."}\n\n`
   - Terminal event: `data: [DONE]\n\n`

#### Acceptance Criteria:

##### Scenario 4.1: Natural Language Semantic Search Query (pgvector Cosine Distance)
- **Given** orders in the database with indexed embeddings:
  - Order 1: "2 Ergonomic Mechanical Keyboards and Wrist Rest"
  - Order 2: "Organic Colombian Whole Bean Coffee 1kg"
- **When** a client issues `GET /api/ai/orders/search?q=ergonomic%20typing%20gear&topK=5`
- **Then** the system vectorizes `"ergonomic typing gear"` into a query vector
- **And** executes an HNSW cosine distance query against `order_embeddings`
- **And** Order 1 is returned as the top result with `similarityScore > 0.70`
- **And** Order 2 has a lower similarity score or is filtered out.

##### Scenario 4.2: Structured AI Synthesis Response
- **Given** a successful semantic search matching 2 related hardware orders
- **When** `IChatClient` completes the synthesis prompt
- **Then** the JSON response returned to the client matches:
  ```json
  {
    "query": "ergonomic typing gear",
    "topK": 5,
    "matches": [
      {
        "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "indexedContent": "2 Ergonomic Mechanical Keyboards...",
        "similarityScore": 0.8842
      }
    ],
    "aiSynthesis": "Found 1 order matching typing equipment, specifically high-end ergonomic keyboards."
  }
  ```

##### Scenario 4.3: Real-Time Token Streaming via Server-Sent Events (SSE)
- **Given** a client connected to `GET /api/ai/orders/search/stream?q=laptops`
- **When** `IChatClient.CompleteStreamingAsync()` generates token chunks
- **Then** the HTTP response status is `200 OK` with header `Content-Type: text/event-stream`
- **And** tokens are flushed immediately to the client in standard SSE format:
  ```
  data: {"type": "metadata", "matchCount": 1}

  data: {"type": "token", "content": "The"}

  data: {"type": "token", "content": " customer"}

  data: [DONE]
  ```

##### Scenario 4.4: HybridCache Hit Eliminates Redundant Model Calls
- **Given** an identical query `q=ergonomic%20keyboards` submitted 10 seconds after a successful request
- **When** the second request executes
- **Then** `CachingChatClient` returns the cached response directly from `HybridCache`
- **And** zero outbound calls are made to the AI model
- **And** response latency is < 15 milliseconds.

##### Scenario 4.5: Offline Mock AI Execution
- **Given** the system is running in default development mode with `AiOptions:Provider == "Mock"`
- **When** the semantic search endpoint is called
- **Then** `OfflineMockEmbeddingGenerator` produces a valid float array
- **And** `OfflineMockChatClient` returns a simulated synthesis
- **And** the endpoint succeeds with HTTP 200 without network access or cloud API keys.

---

### US-5: Architecture Governance & Quality Gates

**As a** software architect and lead maintainer,  
**I want** automated ArchUnitNET boundary tests, production-grade Linux containerization, and deep health check probes,  
**So that** architectural decay is prevented at compile/test time and the service deploys reliably to Kubernetes.

#### Context & Business Rules:
1. Architecture Invariant Rules (ArchUnitNET in `ServiceTemplate.ArchitectureTests`):
   - `Domain` must not depend on `Application`, `Infrastructure`, `Api`, or `Contracts`.
   - `Application` must only depend on `Domain` and `Contracts`; must NOT reference `Infrastructure` or `Api`.
   - `Contracts` must be completely independent (0 internal project references).
   - Minimal API route handlers must never return raw EF Core entities directly to clients; must return DTOs or `Results`.
2. Containerization:
   - Multi-stage `Dockerfile` targeting `mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled`.
   - Container executes as non-root user `$APP_UID` on port `8080`.
   - Zero shell (`/bin/sh` does not exist), zero package manager in final runtime layer.
3. Local Infrastructure (`docker-compose.yml`):
   - PostgreSQL 16 with `pgvector` pre-installed (`pgvector/pgvector:pg16`).
   - RabbitMQ 3.13 with management plugin (`rabbitmq:3-management-alpine`).
   - Healthcheck configurations for both dependencies.
4. Health Probes:
   - `/health/live`: Fast liveness check (responds `200 OK` if process is running).
   - `/health/ready`: Deep readiness check validating PostgreSQL socket & `vector` extension, and RabbitMQ connectivity.

#### Acceptance Criteria:

##### Scenario 5.1: ArchUnitNET Invariant Rule Enforcement
- **Given** the `ServiceTemplate.ArchitectureTests` suite
- **When** a developer runs `dotnet test tests/ServiceTemplate.ArchitectureTests`
- **Then** all architectural boundary tests pass:
  - `Domain_Should_Not_Depend_On_Any_Other_Project`
  - `Application_Should_Not_Depend_On_Infrastructure_Or_Api`
  - `Contracts_Should_Have_No_Internal_Project_Dependencies`
  - `Controllers_And_Endpoints_Should_Not_Expose_Domain_Entities`

##### Scenario 5.2: CI Quality Gate Failure on Boundary Violation
- **Given** a developer accidentally introduces a direct reference from `Domain` to `Infrastructure` or adds an infrastructure using statement in a Domain entity
- **When** `dotnet test` executes
- **Then** ArchUnitNET throws an assertion failure specifying the offending class and dependency
- **And** the test execution exits with a non-zero exit code, breaking the build.

##### Scenario 5.3: Production Chiseled Ubuntu Dockerfile Build & Execution
- **Given** Docker installed on Ubuntu Linux
- **When** `docker build -f docker/Dockerfile -t service-template:test .` is executed
- **Then** the multi-stage build completes successfully
- **And** the final image size is under 150 MB
- **And** inspecting the container confirms it runs as non-root (`USER 1654` or `$APP_UID`)
- **And** running `/bin/sh` inside the image fails because no shell binary exists in the chiseled base.

##### Scenario 5.4: Liveness and Readiness Health Probes
- **Given** the service running alongside Docker Compose infrastructure
- **When** an HTTP GET is performed on `/health/live`
- **Then** the response is HTTP `200 OK` with `{"status": "Healthy"}`
- **When** an HTTP GET is performed on `/health/ready` while PostgreSQL and RabbitMQ are up
- **Then** the response is HTTP `200 OK` with individual check details (`postgres`: Healthy, `rabbitmq`: Healthy)
- **When** PostgreSQL is stopped
- **Then** `/health/ready` immediately returns HTTP `503 Service Unavailable` with `"postgres": "Unhealthy"`.

##### Scenario 5.5: Interactive OpenAPI Documentation via Scalar
- **Given** the service running in `Development` environment
- **When** a user navigates to `/scalar/v1` in a web browser
- **Then** the modern Scalar interactive API documentation loads
- **And** displays all endpoints for `/api/orders` and `/api/ai` with complete request/response schemas.

---

## 4. Definition of Done (DoD) for Phase 3 Implementation

Before any pull request or implementation slice can be approved by the QA & Code Reviewer in Phase 4, the Full-Stack & AI Engineer must verify all items in this Definition of Done:

### 4.1 Architecture & Code Hygiene
- [ ] **Strict Layer Isolation:** Clean project reference boundaries are preserved without circular or prohibited dependencies.
- [ ] **Zero Warnings:** Solution compiles under `net9.0` with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and zero compiler warnings.
- [ ] **Nullable Safety:** `<Nullable>enable</Nullable>` is enforced across all projects with zero unhandled null reference warnings.
- [ ] **No Raw Entities in API:** No EF Core entities are returned across Minimal API endpoints; all responses are typed DTOs.

### 4.2 Messaging & Outbox Consistency
- [ ] **Transactional Outbox Enlistment:** Every domain mutation publishing an integration event does so within an atomic EF Core transaction via Wolverine.
- [ ] **Idempotent Inbox Consumption:** All consumers of external RabbitMQ queues declare `.UseDurableInbox()` and tolerate duplicate redeliveries.
- [ ] **Dead Letter Policy:** Poison messages route to a designated `.poison-dlq` after 3 failed retries.

### 4.3 AI & Persistence Integrity
- [ ] **pgvector Co-location:** Relational entities and vector embeddings reside in the same PostgreSQL database using EF Core HNSW indexing (`vector_cosine_ops`).
- [ ] **Standardized AI Interfaces:** All AI operations interact strictly via `Microsoft.Extensions.AI` (`IChatClient`, `IEmbeddingGenerator`).
- [ ] **Zero-Key First Run:** Default configuration uses `OfflineMockChatClient` and `OfflineMockEmbeddingGenerator`, allowing full build, run, and test without external API credentials.
- [ ] **Resilience Decorators:** Remote AI clients are wrapped in Polly v8 retries, circuit breakers, and OpenTelemetry instrumentation.

### 4.4 Automated Testing & Verification
- [ ] **Unit Tests:** All business logic, domain invariants, and FluentValidation rules have 100% passing xUnit unit tests.
- [ ] **Architecture Tests:** ArchUnitNET tests pass 100% in `ServiceTemplate.ArchitectureTests`.
- [ ] **Integration Tests:** Testcontainers-based tests verify end-to-end flow with real PostgreSQL (pgvector) and RabbitMQ instances.

### 4.5 Packaging & Operational Readiness
- [ ] **`dotnet new` Template:** `.template.config/template.json` is validated, and instantiating the template with `-n` creates a cleanly compiling project.
- [ ] **Chiseled Container:** Dockerfile builds a minimal, non-root Ubuntu Chiseled image.
- [ ] **Observability & Probes:** `/health/live`, `/health/ready`, and OpenTelemetry trace exporter are fully configured.

---

## 5. Traceability Matrix

| Requirement / Architecture Component | Covered In User Story | Verifying Test Suite |
| :--- | :--- | :--- |
| **`dotnet new` Template Config** | US-1 | `dotnet new` execution verification & template test script |
| **Offline Mock AI Provider** | US-1, US-4 | `ServiceTemplate.UnitTests/Ai/OfflineMockAiTests.cs` |
| **Order Aggregate & Domain Events** | US-2, US-3 | `ServiceTemplate.UnitTests/Domain/OrderTests.cs` |
| **FluentValidation Pipeline** | US-2 | `ServiceTemplate.UnitTests/Application/CreateOrderValidatorTests.cs` |
| **Wolverine Transactional Outbox** | US-2 | `ServiceTemplate.IntegrationTests/Slices/CreateOrderIntegrationTests.cs` |
| **Wolverine Durable Inbox & Deduplication**| US-3 | `ServiceTemplate.IntegrationTests/Slices/PaymentProcessingIntegrationTests.cs` |
| **RabbitMQ Exchange & DLQ Routing** | US-2, US-3 | `ServiceTemplate.IntegrationTests/Messaging/WolverineRabbitMqTests.cs` |
| **pgvector HNSW Index & Cosine Query** | US-4 | `ServiceTemplate.IntegrationTests/Slices/SemanticSearchIntegrationTests.cs` |
| **`IChatClient` SSE Streaming Endpoint** | US-4 | `ServiceTemplate.IntegrationTests/Ai/StreamingEndpointTests.cs` |
| **ArchUnitNET Boundary Rules** | US-5 | `ServiceTemplate.ArchitectureTests/BoundaryTests.cs` |
| **Ubuntu Chiseled Dockerfile** | US-5 | Docker container build & non-root verification |
| **Health Probes (`/health/*`)** | US-5 | Alba HTTP Scenario Tests |

---

## 6. Phased Implementation Plan (Handoff to Full-Stack & AI Engineer)

The implementation work for **Phase 3** is structured into five sequential, bite-sized tasks to maintain momentum and ensure test-driven progress:

```
[Task 1: Solution Scaffolding & Core Domain]
- Create Solution and 5 src + 3 test .csproj files targeting net9.0
- Configure .template.config/template.json
- Implement Domain (Order, OrderItem, ValueObjects, Domain Events)
- Implement UnitTests for Domain

[Task 2: Application Slices & Contracts]
- Define Contracts (OrderCreatedIntegrationEvent, OrderPaymentReceived)
- Implement CreateOrder Slice (Command, Validator, Wolverine Handler)
- Implement ProcessPayment Slice (Consumer Handler)
- Implement SearchOrders Slice (Query, Handler)

[Task 3: Infrastructure, pgvector & Wolverine Persistence]
- Configure EF Core ApplicationDbContext with pgvector extension & HNSW indices
- Configure Wolverine with PostgreSQL Outbox/Inbox and RabbitMQ transport
- Implement Microsoft.Extensions.AI decorator chain and OfflineMock providers
- Configure OpenTelemetry and HybridCache

[Task 4: Minimal APIs, SSE & Health Endpoints]
- Implement OrderEndpoints (POST /api/orders)
- Implement AiEndpoints (GET /api/ai/orders/search and SSE /search/stream)
- Implement RFC 7807 GlobalExceptionHandler
- Configure Scalar OpenAPI documentation and /health/live, /health/ready

[Task 5: Architecture Tests, Docker & Integration Suite]
- Implement ArchUnitNET BoundaryTests & CleanArchitectureTests
- Create multi-stage Ubuntu Chiseled Dockerfile & docker-compose.yml
- Create Testcontainers integration test suite (Postgres + RabbitMQ)
- Execute end-to-end verification and handoff to QA
```

---

*Sign-off: Product Manager (Phase 2 Complete - Ready for Phase 3 Implementation)*
