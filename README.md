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
Complete documentation, architecture diagrams, step-by-step guides, and operations runbooks are located in the sibling directory:
👉 `../Documention`
