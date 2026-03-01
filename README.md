# TC.Agro Farm Service 🌾

> Microservice for managing agricultural properties, plots, and sensors in the TC.Agro Solutions platform.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet) ![C#](https://img.shields.io/badge/C%23-14.0-239120?style=flat-square&logo=csharp) ![Build](https://img.shields.io/badge/build-passing-44cc11?style=flat-square) ![Tests](https://img.shields.io/badge/tests-247%20passing-44cc11?style=flat-square) ![Coverage](https://img.shields.io/badge/coverage-92%25-44cc11?style=flat-square) ![License](https://img.shields.io/badge/license-MIT-4078c0?style=flat-square)

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Architecture](#-architecture)
- [Technology Stack](#-technology-stack)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
- [Configuration](#-configuration)
- [API Endpoints](#-api-endpoints)
- [Messaging](#-messaging)
- [Project Structure](#-project-structure)
- [Running Tests](#-running-tests)
- [Docker](#-docker)
- [Health Checks](#-health-checks)
- [Observability](#-observability)
- [License](#-license)

---

## 🎯 Overview

The **Farm Service** manages the core agricultural resources across the platform:

- **Properties** — farm properties owned by producers
- **Plots** — agricultural plots (talhões) within properties, each with a crop type
- **Sensors** — IoT sensors installed in plots, with full operational lifecycle
- **Owners** — denormalized snapshot of users from Identity Service for fast lookups

It:
- ✅ **Provides REST API** for resource management with pagination and filtering
- ✅ **Enforces domain rules** via rich DDD aggregates (PropertyAggregate, PlotAggregate, SensorAggregate)
- ✅ **Publishes integration events** when resources change, consumed by Sensor Ingest and Analytics services
- ✅ **Consumes user events** from Identity Service to maintain OwnerSnapshot cache
- ✅ **Caches read queries** with FusionCache (L1 in-memory + L2 Redis)
- ✅ **Ensures event reliability** with Wolverine Transactional Outbox Pattern

---

## 🏗️ Architecture

Hexagonal Architecture (Ports & Adapters) with Clean Architecture layers:

```
┌────────────────────────────────────────────────────────┐
│  Inbound Adapters                                      │
│  └── FastEndpoints (REST API)                          │
│  └── Wolverine Message Handlers (event consumers)      │
├────────────────────────────────────────────────────────┤
│  Core                                                  │
│  ├── Application (Commands, Queries, Handlers)         │
│  └── Domain (Aggregates, Value Objects, Events)        │
├────────────────────────────────────────────────────────┤
│  Outbound Adapters                                     │
│  └── EF Core + PostgreSQL (persistence)                │
│  └── Wolverine + RabbitMQ (messaging)                  │
│  └── FusionCache + Redis (caching)                     │
└────────────────────────────────────────────────────────┘
```

**Patterns:** Hexagonal Architecture · DDD · CQRS · Outbox Pattern · Snapshot Pattern · Repository Pattern · Result Pattern

---

## 🛠️ Technology Stack

| Category | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| API | FastEndpoints 7.2 |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL 16 |
| Cache | FusionCache 2.0 + Redis 7 |
| Messaging | WolverineFx 5.15 + RabbitMQ 4 |
| Observability | OpenTelemetry · Serilog · Prometheus |
| Validation | FluentValidation 12 · Ardalis.Result |
| Testing | xUnit v3 · FakeItEasy · FastEndpoints.Testing |

---

## 📦 Prerequisites

```bash
dotnet --version   # 10.0.x
docker --version   # 24.0.x or higher
```

**Shared packages** (from `tc-agro-common`): `TC.Agro.Contracts`, `TC.Agro.Messaging`, `TC.Agro.SharedKernel`

---

## 🚀 Getting Started

```bash
git clone https://github.com/rdpresser/tc-agro-farm-service.git
cd tc-agro-farm-service

# Start infrastructure (PostgreSQL, Redis, RabbitMQ)
docker compose up -d

# Apply migrations
dotnet ef database update --project src/Adapters/Inbound/TC.Agro.Farm.Service

# Run the service
dotnet run --project src/Adapters/Inbound/TC.Agro.Farm.Service
```

**Verify:**
```bash
curl http://localhost:5002/health/ready
# open http://localhost:5002/swagger
```

---

## ⚙️ Configuration

```json
// appsettings.Development.json (key fields)
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=farm_db;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "Messaging": {
    "RabbitMQ": {
      "Host": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest"
    }
  }
}
```

**Environment variables (Docker/Kubernetes):**
```bash
export ConnectionStrings__DefaultConnection="Host=postgres;Database=farm_db;Username=postgres;Password=${DB_PASSWORD}"
export ConnectionStrings__Redis="redis:6379"
export Messaging__RabbitMQ__Host=rabbitmq
```

---

## 🔌 API Endpoints

All endpoints are served under the `/api` prefix and require **JWT Bearer Token**.

### Properties

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/properties` | Create a new property |
| `GET` | `/api/properties` | List properties (paginated, filterable) |
| `GET` | `/api/properties/{id}` | Get property by ID |
| `PUT` | `/api/properties/{id}` | Update property |
| `GET` | `/api/properties/{id}/plots` | List plots for a property (paginated) |

### Plots

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/plots` | Create a new plot within a property |
| `GET` | `/api/plots` | List plots (paginated, filterable) |
| `GET` | `/api/plots/{id}` | Get plot by ID |
| `GET` | `/api/plots/{id}/sensors` | List sensors installed in a plot |

> Note: There is no generic `PUT /api/plots/{id}` endpoint. Plot attributes are set at creation time.

### Sensors

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/sensors` | Register a new IoT sensor in a plot |
| `GET` | `/api/sensors` | List sensors (paginated, filterable by property/plot/status) |
| `GET` | `/api/sensors/{id}` | Get sensor by ID |
| `PUT` | `/api/sensors/{id}/status-change` | Change operational status (Active / Maintenance / Faulty / Inactive) |
| `DELETE` | `/api/sensors/{id}` | Deactivate sensor (soft-delete) |

> Note: There is no generic `PUT /api/sensors/{id}` endpoint. Sensor changes go through dedicated status-change and deactivate endpoints.

### Owners

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/owners` | List active owners (synced from Identity Service) |

---

## 📨 Messaging

The Farm Service is a key integration point — it both **publishes** and **consumes** events via Wolverine.

### Published Events

The Farm Service publishes **6 integration events**:

| Event | Trigger | Key Consumers |
|---|---|---|
| `PropertyCreatedIntegrationEvent` | Property created | Analytics Service |
| `PropertyUpdatedIntegrationEvent` | Property updated | Analytics Service |
| `PlotCreatedIntegrationEvent` | Plot created | Analytics Service |
| `SensorRegisteredIntegrationEvent` | Sensor registered | **Sensor Ingest**, Analytics |
| `SensorOperationalStatusChangedIntegrationEvent` | Status changed | **Sensor Ingest**, Analytics |
| `SensorDeactivatedIntegrationEvent` | Sensor deactivated | **Sensor Ingest**, Analytics |

### Consumed Events

| Event | Source | Action |
|---|---|---|
| `UserRegisteredIntegrationEvent` | Identity Service | Create `OwnerSnapshot` |
| `UserUpdatedIntegrationEvent` | Identity Service | Update `OwnerSnapshot` |
| `UserDeactivatedIntegrationEvent` | Identity Service | Deactivate `OwnerSnapshot` |

### Event Flow

```
Identity Service → UserRegistered / Updated / Deactivated
         ↓
    Farm Service (OwnerSnapshotHandler)
    └── maintains OwnerSnapshot table for fast lookups
         ↓
    Domain operations (Properties, Plots, Sensors)
         ↓
    6 resource events → RabbitMQ (Wolverine Outbox)
         ↓
    ├── Sensor Ingest Service (sensor lifecycle events only)
    └── Analytics Service (all resource events)
```

---

## 📂 Project Structure

```
tc-agro-farm-service/
├── src/
│   ├── Core/
│   │   ├── TC.Agro.Farm.Domain/
│   │   │   ├── Aggregates/
│   │   │   │   ├── PropertyAggregate.cs
│   │   │   │   ├── PlotAggregate.cs             # includes CropType
│   │   │   │   └── SensorAggregate.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── PropertyName.cs
│   │   │   │   ├── Address.cs
│   │   │   │   ├── Coordinates.cs               # GPS lat/lon
│   │   │   │   ├── PlotName.cs
│   │   │   │   ├── SensorLabel.cs
│   │   │   │   └── SensorOperationalStatus.cs   # Active, Maintenance, Faulty, Inactive
│   │   │   └── Snapshots/
│   │   │       └── OwnerSnapshot.cs             # denormalized from Identity events
│   │   │
│   │   └── TC.Agro.Farm.Application/
│   │       ├── UseCases/
│   │       │   ├── Properties/                  # Create, Update, GetById, GetList
│   │       │   ├── Plots/                       # Create, GetById, GetList
│   │       │   └── Sensors/                     # Create, ChangeStatus, Deactivate, GetById, GetList
│   │       ├── MessageBrokerHandlers/
│   │       │   └── OwnerSnapshotHandler.cs      # consumes Identity events
│   │       └── Abstractions/Ports/
│   │           ├── IPropertyAggregateRepository.cs
│   │           ├── IPlotAggregateRepository.cs
│   │           ├── ISensorAggregateRepository.cs
│   │           ├── IPropertyReadStore.cs
│   │           ├── IPlotReadStore.cs
│   │           └── ISensorReadStore.cs
│   │
│   └── Adapters/
│       ├── Inbound/TC.Agro.Farm.Service/
│       │   ├── Endpoints/
│       │   │   ├── Properties/
│       │   │   ├── Plots/
│       │   │   ├── Sensors/
│       │   │   └── Owners/
│       │   ├── Telemetry/
│       │   │   ├── FarmMetrics.cs               # custom Prometheus metrics
│       │   │   └── ActivitySourceFactory.cs
│       │   └── Program.cs
│       │
│       └── Outbound/TC.Agro.Farm.Infrastructure/
│           ├── ApplicationDbContext.cs
│           ├── Repositories/
│           │   ├── PropertyAggregateRepository.cs
│           │   ├── PlotAggregateRepository.cs
│           │   ├── SensorAggregateRepository.cs
│           │   ├── PropertyReadStore.cs
│           │   ├── PlotReadStore.cs
│           │   └── SensorReadStore.cs
│           ├── Configurations/                  # EF Core entity configs
│           └── Migrations/
│
└── test/TC.Agro.Farm.Tests/
    ├── Domain/
    │   ├── Aggregates/   # PropertyAggregate, PlotAggregate, SensorAggregate
    │   └── ValueObjects/ # Address, Coordinates, SensorOperationalStatus...
    ├── Application/
    │   ├── UseCases/     # Create/Update/Get per aggregate
    │   └── MessageBrokerHandlers/ # OwnerSnapshotHandler
    └── Builders/         # PropertyAggregateBuilder, PlotAggregateBuilder, SensorAggregateBuilder
```

---

## 🧪 Running Tests

```bash
dotnet test
dotnet test --verbosity normal
dotnet test --filter "FullyQualifiedName~Domain"
dotnet test --filter "FullyQualifiedName~Application"
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🐳 Docker

```bash
# Build
docker build -t tc-agro-farm-service .

# Run
docker run -p 5002:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=farm_db;Username=postgres;Password=postgres" \
  -e ConnectionStrings__Redis="redis:6379" \
  --network tc-agro-network \
  tc-agro-farm-service
```

---

## 🏥 Health Checks

| Endpoint | Purpose |
|---|---|
| `/health` | Overall health (PostgreSQL, Redis) |
| `/health/ready` | Kubernetes readiness probe |
| `/health/live` | Kubernetes liveness probe |

---

## 📊 Observability

- **Metrics:** `/metrics` — Prometheus format (HTTP, DB queries, Wolverine, FusionCache, custom farm registration counters)
- **Tracing:** OTLP export, W3C Trace Context, `X-Correlation-Id` propagated through all requests and messages
- **Logging:** Serilog structured logs → console + OTLP Collector → Grafana Loki

**Local access:**
- Grafana: `http://localhost:3000` (admin/admin)
- Prometheus: `http://localhost:9090`

---

## 📚 Related Services

- **[Identity Service](../identity-service/README.md)** — publishes user events consumed here
- **[Sensor Ingest Service](../sensor-ingest-service/README.md)** — consumes sensor lifecycle events
- **[Analytics Service](../analytics-worker/README.md)** — consumes all resource events

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

> Part of TC Agro Solutions — Hackathon 8NETT · FIAP Postgraduate · Phase 5
