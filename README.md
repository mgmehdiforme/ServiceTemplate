# Enterprise Microservice Chassis Template (.NET 9)

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Architecture: Vertical Slice](https://img.shields.io/badge/Architecture-Vertical%20Slice-success.svg)]()
[![Messaging: Wolverine](https://img.shields.io/badge/Messaging-Wolverine-orange.svg)](https://wolverine.netlify.app/)
[![Vector: pgvector](https://img.shields.io/badge/Vector-pgvector-blue.svg)]()

Production-ready enterprise-grade microservice base template for the .NET ecosystem. Designed to eliminate the "blank canvas" trap by standardizing cross-cutting concerns: transactional outbox/inbox messaging, resilient integration pipelines, co-located vector search, OpenTelemetry observability, and AI primitives.

---

## Key Highlights

- **Runtime & Language:** .NET 9 LTS (C# 13) with ASP.NET Core Minimal APIs.
- **Architecture:** Vertical Slice Architecture partitioned across `Api`, `Application`, `Domain`, `Infrastructure`, and `Contracts`.
- **Reliable Messaging:** [Wolverine](https://wolverine.netlify.app/) with PostgreSQL EF Core Transactional Outbox/Inbox and RabbitMQ transport.
- **Persistence & Vectors:** Entity Framework Core with PostgreSQL (`Npgsql` + `pgvector`), housing transactional tables and vector embeddings in the same private database.
- **Native AI Primitives:** `Microsoft.Extensions.AI` (`IChatClient`, `IEmbeddingGenerator`) + `Microsoft.SemanticKernel` tool integration, with offline mock fallback.
- **Resilience:** `Microsoft.Extensions.Resilience` (Polly v8) circuit breakers, retries, and timeouts.
- **Observability:** OpenTelemetry metrics, structured logging, and distributed tracing.
- **Architectural Guardrails:** `ArchUnitNET` automated architecture unit tests enforcing boundary isolation.
- **Template Distribution:** Standard `dotnet new` template package.

---

## Quickstart

### 1. Install Template
```bash
dotnet new install .
```

### 2. Scaffold a New Microservice
```bash
dotnet new microservice-chassis -n OrderingService
cd OrderingService
dotnet run --project src/OrderingService.Api
```

---

## Detailed Documentation
Complete documentation, architecture blueprints, step-by-step developer guides, and messaging specifications are located in the [Documention](Documention/) folder:

- 📐 [Architecture Specification](Documention/01_ARCHITECTURE_SPECIFICATION.md) - Deep architectural design, component topologies, trade-off matrices, and technology selections.
- 📋 [Product Requirements Specification](Documention/02_PRODUCT_REQUIREMENTS_SPECIFICATION.md) - Functional specifications, user stories, and acceptance criteria.
- 🛠️ [Developer Guide](Documention/03_DEVELOPER_GUIDE.md) - Step-by-step guide to adding vertical slices, offline mock AI, Docker dev stacks, migrations, and deployment.
- 📡 [API & Messaging Reference](Documention/04_API_AND_MESSAGING_REFERENCE.md) - HTTP Minimal API schemas, SSE streaming, RFC 7807 problem details, Wolverine outbox/inbox schema, and RabbitMQ topologies.
- ✅ [QA Verification Report](Documention/05_QA_VERIFICATION_REPORT.md) - Automated test run logs, static checks, template installation audit, and security review sign-off.

---

## Running the Automated Test Suite

The solution comes with an exhaustive test suite guaranteeing 100% pass rates across all layers:

```bash
dotnet test ServiceTemplate.sln
```

- **Domain & Application Unit Tests:** 36 tests covering aggregates, value objects, domain events, validators, and resilient AI pipeline decorators.
- **ArchUnitNET Boundary Tests:** 5 automated architectural tests verifying strict layer separation and dependency inversion.
- **Integration Tests:** 8 end-to-end slice tests with in-memory execution and Testcontainers fallback covering REST endpoints, SSE streams, validation errors, and Kubernetes probes.
