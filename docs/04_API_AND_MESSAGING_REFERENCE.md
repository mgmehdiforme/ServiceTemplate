# API & Messaging Reference Specification

This document serves as the comprehensive technical reference for all HTTP Minimal API endpoints, Server-Sent Events (SSE) streaming protocols, RFC 7807 problem details, Wolverine PostgreSQL outbox schemas, RabbitMQ broker topologies, and resilience policies in the **Enterprise Microservice Chassis Template**.

---

## Table of Contents
1. [Interactive API Explorer (OpenAPI & Scalar)](#1-interactive-api-explorer-openapi--scalar)
2. [HTTP Minimal API Catalog](#2-http-minimal-api-catalog)
   - [POST /api/orders](#21-post-apiorders)
   - [GET /api/orders/{id}](#22-get-apiordersid)
   - [GET /api/ai/orders/search](#23-get-apiaiorderssearch)
   - [GET /api/ai/orders/search/stream (SSE)](#24-get-apiaiorderssearchstream-server-sent-events)
   - [GET /health/live](#25-get-healthlive)
   - [GET /health/ready](#26-get-healthready)
3. [RFC 7807 ProblemDetails Error Specifications](#3-rfc-7807-problemdetails-error-specifications)
4. [Wolverine PostgreSQL Transactional Outbox Schema](#4-wolverine-postgresql-transactional-outbox-schema)
5. [RabbitMQ Message Broker Topology](#5-rabbitmq-message-broker-topology)
6. [Integration Event Contracts Catalog](#6-integration-event-contracts-catalog)
7. [Poison Queue, Retry & Circuit Breaker Policies](#7-poison-queue-retry--circuit-breaker-policies)

---

## 1. Interactive API Explorer (OpenAPI & Scalar)

The chassis exposes native OpenAPI 3.0 document generation and the interactive [Scalar](https://scalar.com/) API client:

- **Scalar GUI (Development):** `http://localhost:5000/scalar/v1`
- **Raw OpenAPI 3.0 JSON:** `http://localhost:5000/openapi/v1.json`

Scalar provides request testing, syntax-highlighted code samples in multiple languages (cURL, C#, Python, TypeScript), and schema validation directly in the browser.

---

## 2. HTTP Minimal API Catalog

### 2.1 POST /api/orders

Creates a new customer order, enforces domain business rules and validation constraints, persists the aggregate, and enlists an `OrderCreatedIntegrationEvent` into the Wolverine transactional outbox.

- **URL:** `/api/orders`
- **Method:** `POST`
- **Content-Type:** `application/json`
- **Accept:** `application/json, application/problem+json`

#### Request Body Schema
| Field | Type | Required | Description | Constraints |
| :--- | :--- | :--- | :--- | :--- |
| `customerId` | `string` | **Yes** | Unique identifier of the purchasing customer. | Max 100 characters, non-empty. |
| `items` | `array` | **Yes** | List of items to purchase. | Min 1 item required. |
| `items[].sku` | `string` | **Yes** | Stock keeping unit identifier. | Max 50 characters, non-empty. |
| `items[].productName` | `string` | **Yes** | Human-readable product name. | Max 200 characters, non-empty. |
| `items[].quantity` | `integer`| **Yes** | Number of units ordered. | Greater than 0. |
| `items[].unitPrice` | `decimal`| **Yes** | Price per unit. | Greater than 0. |
| `shippingAddress.street` | `string` | **Yes** | Street address. | Max 200 characters, non-empty. |
| `shippingAddress.city` | `string` | **Yes** | City. | Max 100 characters, non-empty. |
| `shippingAddress.country` | `string` | **Yes** | Country. | Max 100 characters, non-empty. |
| `currency` | `string` | No | Currency code (defaults to `"USD"`). | ISO 4217 3-letter code. |

#### Request Example
```json
{
  "customerId": "cust_987654",
  "items": [
    {
      "sku": "KB-MECH-RGB",
      "productName": "Ergonomic Mechanical Keyboard (Brown Switches)",
      "quantity": 2,
      "unitPrice": 149.99
    },
    {
      "sku": "ACC-WRIST-01",
      "productName": "Memory Foam Ergonomic Wrist Rest",
      "quantity": 1,
      "unitPrice": 29.99
    }
  ],
  "shippingAddress": {
    "street": "10 Downing Street",
    "city": "London",
    "country": "United Kingdom"
  },
  "currency": "USD"
}
```

#### Response: 201 Created
- **Headers:** `Location: /api/orders/e3b0c442-98fc-1c14-9afb-4c8996fb9242`
```json
{
  "id": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "customerId": "cust_987654",
  "totalAmount": 329.97,
  "currency": "USD",
  "status": "Pending"
}
```

---

### 2.2 GET /api/orders/{id}

Retrieves full order aggregate details by its unique identifier.

- **URL:** `/api/orders/{id}`
- **Method:** `GET`
- **Route Parameters:**
  - `id` (`guid`, required): The order unique identifier.
- **Accept:** `application/json, application/problem+json`

#### Response: 200 OK
```json
{
  "id": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "customerId": "cust_987654",
  "status": "Pending",
  "totalAmount": 329.97,
  "currency": "USD",
  "createdAt": "2026-10-27T17:30:00+00:00",
  "updatedAt": null,
  "shippingAddress": {
    "street": "10 Downing Street",
    "city": "London",
    "country": "United Kingdom"
  },
  "items": [
    {
      "id": "a1b2c3d4-e5f6-7890-1234-56789abcdef0",
      "sku": "KB-MECH-RGB",
      "productName": "Ergonomic Mechanical Keyboard (Brown Switches)",
      "quantity": 2,
      "unitPrice": 149.99,
      "total": 299.98
    },
    {
      "id": "b2c3d4e5-f6a1-7890-1234-56789abcdef1",
      "sku": "ACC-WRIST-01",
      "productName": "Memory Foam Ergonomic Wrist Rest",
      "quantity": 1,
      "unitPrice": 29.99,
      "total": 29.99
    }
  ]
}
```

#### Response: 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Order Not Found",
  "status": 404,
  "detail": "Order with id '00000000-0000-0000-0000-000000000000' was not found.",
  "instance": "/api/orders/00000000-0000-0000-0000-000000000000"
}
```

---

### 2.3 GET /api/ai/orders/search

Performs co-located vector embedding generation and cosine similarity search in `order_embeddings` (via `pgvector`), then generates a natural language synthesis of the matched purchase intents using `IChatClient`.

- **URL:** `/api/ai/orders/search`
- **Method:** `GET`
- **Query Parameters:**
  - `q` (`string`, required): Free-text semantic search query.
  - `topK` (`integer`, optional, default: 5): Maximum number of top matching orders to retrieve.

#### Request Example
```http
GET /api/ai/orders/search?q=mechanical%20keyboards&topK=3 HTTP/1.1
Host: localhost:5000
Accept: application/json
```

#### Response: 200 OK
```json
{
  "query": "mechanical keyboards",
  "topK": 3,
  "matches": [
    {
      "orderId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
      "indexedContent": "Customer cust_987654 ordered 2x Ergonomic Mechanical Keyboard (Brown Switches) with Memory Foam Ergonomic Wrist Rest",
      "similarityScore": 0.9412
    }
  ],
  "aiSynthesis": "Found 1 matching customer order indicating demand for ergonomic mechanical keyboards and accessories."
}
```

---

### 2.4 GET /api/ai/orders/search/stream (Server-Sent Events)

Streams AI synthesis tokens in real time using HTTP chunked Server-Sent Events (`text/event-stream`).

- **URL:** `/api/ai/orders/search/stream`
- **Method:** `GET`
- **Query Parameters:**
  - `q` (`string`, required): Free-text semantic search query.
- **Headers:** `Accept: text/event-stream`

#### Response: 200 OK (`text/event-stream`)
```http
HTTP/1.1 200 OK
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive

data: {"chunk":"Searching"}

data: {"chunk":" orders"}

data: {"chunk":" for"}

data: {"chunk":" 'keyboards'..."}

data: {"chunk":" Analysis complete: matched 1 order."}

data: [DONE]

```

---

### 2.5 GET /health/live

Kubernetes liveness probe. Checks whether the ASP.NET Core process is responsive.

- **URL:** `/health/live`
- **Method:** `GET`
- **Response: 200 OK**
```json
{
  "status": "Healthy"
}
```

---

### 2.6 GET /health/ready

Kubernetes readiness probe. Validates that downstream dependencies (PostgreSQL database connection, pgvector extension, and RabbitMQ broker) are healthy and reachable before traffic is routed to the pod.

- **URL:** `/health/ready`
- **Method:** `GET`

#### Response: 200 OK (Healthy)
```json
{
  "status": "Healthy",
  "checks": {
    "postgres": "Healthy",
    "rabbitmq": "Healthy"
  },
  "durationMs": 4.12
}
```

#### Response: 503 Service Unavailable (Unhealthy)
```json
{
  "status": "Unhealthy",
  "checks": {
    "postgres": "Healthy",
    "rabbitmq": "Unhealthy"
  },
  "durationMs": 2003.5
}
```

---

## 3. RFC 7807 ProblemDetails Error Specifications

The chassis standardizes all API errors under [RFC 7807 Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807) with media type `application/problem+json`.

### 3.1 Validation Error (HTTP 400 Bad Request)
Emitted when incoming payloads fail FluentValidation rules in the Wolverine pipeline:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors field for details.",
  "instance": "/api/orders",
  "errors": {
    "CustomerId": [
      "CustomerId is required and cannot be empty."
    ],
    "Items": [
      "Order must contain at least one item."
    ],
    "ShippingAddress.Street": [
      "Shipping address street is required."
    ]
  }
}
```

### 3.2 Resource Not Found (HTTP 404 Not Found)
Emitted when an entity ID does not exist:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Order Not Found",
  "status": 404,
  "detail": "Order with id '4484931a-e565-4f48-a006-258046b080f3' was not found.",
  "instance": "/api/orders/4484931a-e565-4f48-a006-258046b080f3"
}
```

### 3.3 Domain Invariant Violation (HTTP 422 Unprocessable Entity)
Emitted when domain invariants or state machine transition rules are violated:

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Business Rule Violation",
  "status": 422,
  "detail": "Only pending orders can be marked as paid. Current status: Shipped.",
  "instance": "/api/orders/4484931a-e565-4f48-a006-258046b080f3"
}
```

### 3.4 Internal Server Error (HTTP 500 Internal Server Error)
Emitted for unhandled runtime exceptions. In Development and Testing environments, full stack traces are attached in `detail`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "An unexpected internal server error occurred.",
  "instance": "/api/orders"
}
```

---

## 4. Wolverine PostgreSQL Transactional Outbox Schema

Wolverine persists outgoing integration events and incoming envelopes in PostgreSQL tables under the dedicated schema `wolverine_messages`:

### Outbox Table: `wolverine_messages`
| Column | Type | Description |
| :--- | :--- | :--- |
| `id` | `UUID` (PK) | Unique message envelope identifier. |
| `body` | `BYTEA` | Binary serialized JSON envelope payload. |
| `message_type` | `VARCHAR(250)` | Assembly-qualified type name of the event. |
| `destination` | `VARCHAR(250)` | Target URI (e.g. `rabbitmq://exchange/orders.topic/routing/order.created`). |
| `status` | `VARCHAR(50)` | Lifecycle state: `Scheduled`, `Incoming`, `Handled`. |
| `attempts` | `INTEGER` | Number of execution/delivery attempts. |
| `deliver_by` | `TIMESTAMPTZ` | Scheduled delivery timestamp (null for immediate). |
| `timestamp` | `TIMESTAMPTZ` | Timestamp when envelope was enlisted in outbox. |
| `headers` | `BYTEA` | Serialized dictionary of metadata headers (CorrelationId, TraceParent). |

### Atomicity Guarantee
```sql
BEGIN TRANSACTION;
  -- 1. Persist business state
  INSERT INTO orders (id, customer_id, total_amount, currency, status, version) 
  VALUES ('e3b0c442...', 'cust_123', 329.97, 'USD', 'Pending', 1);

  -- 2. Persist outbox envelope in the SAME transaction
  INSERT INTO wolverine_messages (id, body, destination, message_type, status)
  VALUES ('f8a2...', '\x7b224f726465724964...', 'rabbitmq://exchange/orders.topic', 'OrderCreatedIntegrationEvent', 'Scheduled');
COMMIT;
```

---

## 5. RabbitMQ Message Broker Topology

Wolverine automatically provisions the topic exchange and queue bindings on startup via `AutoProvision()`:

```
[ Microservice Api ]
        │
        ▼ (Wolverine Outbox Publisher)
 ┌───────────────┐
 │ orders.topic  │ (Exchange: topic, durable: true)
 └───────┬───────┘
         │
         ├── Routing Key: "order.created" ──────────> Downstream Subscribers (Billing, Shipping)
         │
         └── Routing Key: "order.payment-received" ──> [ orders.payment-received ] (Queue)
                                                                │
                                                                ▼
                                                    (Wolverine Durable Inbox)
                                                                │
                                                                ▼
                                                    ProcessPaymentConsumer Handler
```

### Topology Details
- **Exchange:** `orders.topic`
  - **Type:** `topic`
  - **Durable:** `true`
  - **Auto-Delete:** `false`
- **Consumer Queue:** `orders.payment-received`
  - **Durable:** `true`
  - **Exclusive:** `false`
  - **Bound To:** `orders.topic` with routing key `order.payment-received`
- **Dead Letter Queue (DLQ):** `wolverine.dead-letter-queue`
  - **Durable:** `true`
  - **Purpose:** Stores poisoned messages that failed all retry attempts.

---

## 6. Integration Event Contracts Catalog

Contracts live in `LogisticsService.Contracts` with **0 external dependencies** and can be packaged into a shared NuGet package for inter-service communication.

### 6.1 OrderCreatedIntegrationEvent
Published to `orders.topic` with routing key `order.created` upon order creation:

```csharp
namespace ServiceTemplate.Contracts.Events;

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt) : IntegrationEvent(OrderId);
```

#### JSON Payload:
```json
{
  "id": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "orderId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "customerId": "cust_987654",
  "totalAmount": 329.97,
  "currency": "USD",
  "createdAt": "2026-10-27T17:30:00+00:00"
}
```

---

### 6.2 OrderPaymentReceived
Consumed from queue `orders.payment-received` when external payment provider approves payment:

```csharp
namespace ServiceTemplate.Contracts.Events;

public sealed record OrderPaymentReceived(
    Guid OrderId,
    string PaymentTransactionId,
    decimal AmountPaid,
    string Currency,
    DateTimeOffset PaidAt) : IntegrationEvent(OrderId);
```

#### JSON Payload:
```json
{
  "id": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "orderId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "paymentTransactionId": "txn_stripe_3N342894jksd",
  "amountPaid": 329.97,
  "currency": "USD",
  "paidAt": "2026-10-27T17:32:15+00:00"
}
```

---

## 7. Poison Queue, Retry & Circuit Breaker Policies

### 7.1 Transient Error Retries
Wolverine handles transient network and database hiccups using progressive cooldown retries:

| Exception Type | Retry Schedule | Next Action |
| :--- | :--- | :--- |
| `NpgsqlException` (DB timeout/disconnect) | `50ms` -> `200ms` -> `1000ms` | Escalate to global policy |
| `TimeoutException` | `100ms` -> `500ms` -> `2000ms` | Escalate to global policy |
| `Any Exception` | `100ms` -> `500ms` -> `2000ms` | Move to Error Queue (`DLQ`) |

### 7.2 Circuit Breaker Configuration
The incoming queue `orders.payment-received` is protected by a circuit breaker:
- **Failure Threshold:** `30%` failure rate over active evaluations.
- **Tracking Window:** `1 minute`.
- **Circuit Trip Action:** Listener pauses consumption for `30 seconds`.
- **Recovery:** Automatically enters half-open state and resumes message consumption once upstream stabilizes.
