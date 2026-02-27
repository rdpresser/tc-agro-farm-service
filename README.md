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
- [Contributing](#-contributing)
- [License](#-license)

---

## 🎯 Overview

The **Farm Service** is responsible for managing the core farm resources:

- **Properties** - Farm properties owned by producers
- **Plots** - Agricultural plots (talhões) within properties
- **Sensors** - IoT sensors installed in plots for monitoring

## 🏗️ Architecture

This service follows **Hexagonal Architecture** (Clean Architecture) principles:

```
┌────────────────────────────────────────────────────────┐
│  Inbound Adapters (API)                                │
│  └── FastEndpoints (REST API)                          │
├────────────────────────────────────────────────────────┤
│  Core                                                  │
│  ├── Application (Use Cases, Commands, Queries)        │
│  └── Domain (Aggregates, Value Objects, Events)        │
├────────────────────────────────────────────────────────┤
│  Outbound Adapters (Infrastructure)                    │
│  └── EF Core, PostgreSQL, Wolverine, Redis             │
└────────────────────────────────────────────────────────┘
```

📖 See [Architecture Documentation](docs/ARCHITECTURE.md) for detailed information.

## 🛠️ Technology Stack

| Category | Technology |
|----------|------------|
| Runtime | .NET 10 |
| API | FastEndpoints |
| Database | PostgreSQL + EF Core |
| Caching | Redis + FusionCache |
| Messaging | Wolverine |
| Observability | OpenTelemetry, Serilog |

## 📦 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [Redis](https://redis.io/) (optional, for distributed caching)
- [Docker](https://www.docker.com/) (optional, for containerized deployment)

## 🚀 Getting Started

### Clone the Repository

```bash
git clone https://github.com/tc-agro-solutions/services.git
cd services/farm-service
```

### Configure Environment

Create a `.env` file in the root directory or set environment variables:

```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=farm_db;Username=postgres;Password=yourpassword

# Redis (optional)
ConnectionStrings__Redis=localhost:6379

# OpenTelemetry (optional)
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

### Run Database Migrations

```bash
cd src/Adapters/Inbound/TC.Agro.Farm.Service
dotnet ef database update
```

### Run the Service

```bash
dotnet run --project src/Adapters/Inbound/TC.Agro.Farm.Service
```

The API will be available at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

### Swagger Documentation

Access the interactive API documentation at:
- `https://localhost:5001/swagger`

---

## ⚙️ Configuration

### Configuration Structure

The project uses ASP.NET Core's hierarchical configuration pattern:

```
appsettings.json (base - empty by default)
├── appsettings.Development.json (local development)
├── appsettings.Production.json (production/cloud)
└── Environment Variables (Docker/Kubernetes - override)
```

### appsettings.Development.json (Example)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=farm_db;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### appsettings.Production.json (Cloud Example)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-db-server.com;Port=5432;Database=farm_db;Username=postgres;Password=${DB_PASSWORD};SSL Mode=Require",
    "Redis": "${REDIS_CONNECTION_STRING}"
  },
  "ApplicationInsights": {
    "ConnectionString": "${APPLICATIONINSIGHTS_CONNECTION_STRING}"
  }
}
```

### Environment Variables (Docker/Kubernetes)

```bash
# Database
export ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=farm_db;Username=postgres;Password=${DB_PASSWORD}"

# Redis
export ConnectionStrings__Redis="redis:6379"

# OpenTelemetry
export OTEL_EXPORTER_OTLP_ENDPOINT="http://otel-collector:4317"

# Observability
export ApplicationInsights__ConnectionString="${APPINSIGHTS_CONN_STRING}"
```

---

## 🔌 API Endpoints

All endpoints are served under the `/api` prefix (for example, `/api/properties`).

### Properties

| Method | Endpoint                     | Description                                                                                 |
|--------|------------------------------|---------------------------------------------------------------------------------------------|
| `POST` | `/api/properties`            | Create a new property (farm).                                                              |
| `GET`  | `/api/properties/{id}`       | Get detailed information about a property by ID.                                           |
| `GET`  | `/api/properties`            | Get a paginated list of properties. Supports filtering and sorting via query parameters.   |
| `PUT`  | `/api/properties/{id}`       | Update an existing property by ID.                                                         |
| `GET`  | `/api/properties/{id}/plots` | Get a paginated list of plots for a given property.                                       |

### Plots

| Method | Endpoint              | Description                                                                               |
|--------|------------------------|-------------------------------------------------------------------------------------------|
| `POST` | `/api/plots`          | Create a new plot within an existing property.                                           |
| `GET`  | `/api/plots/{id}`     | Get detailed information about a plot by ID.                                             |
| `GET`  | `/api/plots`          | Get a paginated list of plots. Supports filtering and sorting via query parameters.      |

> Note: There is currently no generic `PUT /api/plots/{id}` update endpoint implemented.

### Sensors

| Method   | Endpoint                                | Description                                                                                                      |
|----------|-----------------------------------------|------------------------------------------------------------------------------------------------------------------|
| `POST`   | `/api/sensors`                          | Register a new IoT sensor within a plot.                                                                        |
| `GET`    | `/api/sensors/{id}`                     | Get detailed information about a sensor by ID.                                                                  |
| `GET`    | `/api/sensors`                          | Get a paginated list of sensors. Supports filtering by property, plot, type, and status.                        |
| `GET`    | `/api/plots/{id}/sensors`               | Get a paginated list of sensors installed in a specific plot.                                                   |
| `PUT`    | `/api/sensors/{sensorId}/status-change` | Change the operational status of a sensor (for example: Active, Maintenance, Faulty, Inactive).                 |
| `DELETE` | `/api/sensors/{sensorId}`               | Deactivate (soft-delete) a sensor, marking it as inactive and removing it from active queries.                  |

> Note: There is no generic `PUT /api/sensors/{id}` update endpoint; status changes and deactivation use the dedicated endpoints above.

### Owners

| Method | Endpoint       | Description                                                                 |
|--------|----------------|-----------------------------------------------------------------------------|
| `GET`  | `/api/owners`  | Get a paginated list of active owners synchronized from the Identity service. |

## 📨 Messaging

The Farm Service is a **key integration point** in the TC.Agro ecosystem, both **consuming** and **publishing** integration events via **Wolverine** message broker.

### Published Events

The Farm Service publishes **6 integration events** to notify other services about resource lifecycle changes:

#### Properties Events

| Event | Description | Consumers |
|-------|-------------|-----------|
| `PropertyCreatedIntegrationEvent` | Published when a new property (farm) is created. Contains property metadata including ID, name, location, area, and owner association. | Analytics Worker, Reporting Service |
| `PropertyUpdatedIntegrationEvent` | Published when property information is updated (name, location, area). Includes old and new values for audit purposes. | Analytics Worker, Reporting Service |

#### Plots Events

| Event | Description | Consumers |
|-------|-------------|-----------|
| `PlotCreatedIntegrationEvent` | Published when a new plot is created within a property. Contains plot metadata including ID, name, area, crop type, and property association. | Analytics Worker, Reporting Service |

#### Sensors Events

| Event | Description | Consumers |
|-------|-------------|-----------|
| `SensorRegisteredIntegrationEvent` | Published when a new IoT sensor is registered in a plot. Contains sensor metadata including ID, type, label, plot/property associations, and installation details. | **Sensor Ingest Service**, Analytics Worker |
| `SensorOperationalStatusChangedIntegrationEvent` | Published when a sensor's operational status changes (e.g., Active → Maintenance, Faulty, Inactive). Includes sensor ID, old status, new status, timestamp, and optional reason. | **Sensor Ingest Service**, Analytics Worker |
| `SensorDeactivatedIntegrationEvent` | Published when a sensor is deactivated (soft-deleted). Indicates the sensor should no longer accept or process telemetry data. Includes deactivation timestamp and reason. | **Sensor Ingest Service**, Analytics Worker |

### Consumed Events

The Farm Service **consumes** events from the **Identity Service** to maintain an up-to-date cache of owners (users):

| Event | Description | Source Service |
|-------|-------------|----------------|
| `UserRegisteredIntegrationEvent` | Consumed when a new user registers in the Identity Service. Creates an `OwnerSnapshot` for quick lookups. | Identity Service |
| `UserUpdatedIntegrationEvent` | Consumed when user profile information is updated (name, email, phone). Updates the corresponding `OwnerSnapshot`. | Identity Service |
| `UserDeactivatedIntegrationEvent` | Consumed when a user account is deactivated. Marks the `OwnerSnapshot` as inactive. | Identity Service |

### Event Flow Diagram

```
┌──────────────────┐
│ Identity Service │
└────────┬─────────┘
         │ User Events (UserRegistered, UserUpdated, UserDeactivated)
         │
         ▼
    ┌────────────────────────┐
    │   Farm Service         │
    │  ┌──────────────────┐  │
    │  │ Owner Snapshots  │  │ (Cached user data)
    │  └──────────────────┘  │
    │                        │
    │  ┌──────────────────┐  │
    │  │ Domain Logic     │  │ (Properties, Plots, Sensors)
    │  └──────────────────┘  │
    └─────────┬──────────────┘
              │
              │ Resource Events (6 types)
              │ ├─ Property: Created, Updated
              │ ├─ Plot: Created
              │ └─ Sensor: Registered, StatusChanged, Deactivated
              │
              ▼
    ┌───────────────────────────────────┐
    │     RabbitMQ / Wolverine          │
    └─────────┬─────────────────────────┘
              │
              ├──────────────────────────────────────┐
              │                                      │
              ▼                                      ▼
    ┌────────────────────────┐          ┌────────────────────────┐
    │ Sensor Ingest Service  │          │   Analytics Worker     │
    │ (Sensor events only)   │          │ (All resource events)  │
    └────────────────────────┘          └────────────────────────┘
```

### Message Handler Implementation

The Farm Service uses **Wolverine message handlers** to process consumed events:

```csharp
// Example: OwnerSnapshotHandler.cs (consumes Identity Service events)
public class OwnerSnapshotHandler
{
    public async Task Handle(UserRegisteredIntegrationEvent @event, CancellationToken ct)
    {
        // Create OwnerSnapshot for quick lookups in property/plot/sensor creation
        var snapshot = new OwnerSnapshot
        {
            Id = @event.UserId,
            FullName = @event.FullName,
            Email = @event.Email,
            PhoneNumber = @event.PhoneNumber,
            IsActive = true
        };

        await _context.OwnerSnapshots.AddAsync(snapshot, ct);
        await _context.SaveChangesAsync(ct);
    }
}
```

These events ensure **eventual consistency** across services in the distributed TC.Agro ecosystem.

---

## 📂 Project Structure

### Directory Layout

```
tc-agro-farm-service/
├── src/
│   ├── Core/                                           # Domain + Application Logic (Business Rules)
│   │   ├── TC.Agro.Farm.Domain/
│   │   │   ├── Aggregates/
│   │   │   │   ├── PropertyAggregate.cs                # 🏡 Property aggregate root
│   │   │   │   ├── PlotAggregate.cs                    # 🌱 Plot aggregate root
│   │   │   │   ├── SensorAggregate.cs                  # 📡 Sensor aggregate root
│   │   │   │   └── FarmDomainErrors.cs                 # Domain error definitions
│   │   │   ├── ValueObjects/
│   │   │   │   ├── PropertyName.cs                     # Validated property name
│   │   │   │   ├── Address.cs                          # Complete address value object
│   │   │   │   ├── Coordinates.cs                      # GPS coordinates (latitude/longitude)
│   │   │   │   ├── PlotName.cs                         # Validated plot name
│   │   │   │   ├── SensorLabel.cs                      # Validated sensor label
│   │   │   │   └── SensorOperationalStatus.cs          # Active, Maintenance, Faulty, Inactive
│   │   │   ├── Snapshots/
│   │   │   │   └── OwnerSnapshot.cs                    # Cached user data from Identity Service
│   │   │   └── DomainEvents/
│   │   │       ├── PropertyCreatedDomainEvent.cs
│   │   │       ├── PropertyUpdatedDomainEvent.cs
│   │   │       ├── PlotCreatedDomainEvent.cs
│   │   │       ├── SensorRegisteredDomainEvent.cs
│   │   │       ├── SensorStatusChangedDomainEvent.cs
│   │   │       └── SensorDeactivatedDomainEvent.cs
│   │   │
│   │   └── TC.Agro.Farm.Application/
│   │       ├── UseCases/                               # 🎯 CQRS Commands & Queries
│   │       │   ├── Properties/                         # Property use cases
│   │       │   │   ├── Create/CreatePropertyCommandHandler.cs
│   │       │   │   ├── Update/UpdatePropertyCommandHandler.cs
│   │       │   │   ├── GetById/GetPropertyByIdQueryHandler.cs
│   │       │   │   └── GetList/GetPropertiesQueryHandler.cs
│   │       │   ├── Plots/                              # Plot use cases
│   │       │   │   ├── Create/CreatePlotCommandHandler.cs
│   │       │   │   ├── GetById/GetPlotByIdQueryHandler.cs
│   │       │   │   └── GetList/GetPlotsQueryHandler.cs
│   │       │   └── Sensors/                            # Sensor use cases
│   │       │       ├── Create/CreateSensorCommandHandler.cs
│   │       │       ├── ChangeStatus/ChangeSensorStatusCommandHandler.cs
│   │       │       ├── Deactivate/DeactivateSensorCommandHandler.cs
│   │       │       ├── GetById/GetSensorByIdQueryHandler.cs
│   │       │       └── GetList/GetSensorsQueryHandler.cs
│   │       ├── MessageBrokerHandlers/                  # 📨 Event Consumers (Wolverine)
│   │       │   └── OwnerSnapshotHandler.cs             # Consumes Identity Service events
│   │       ├── Abstractions/
│   │       │   ├── Ports/                              # Repository interfaces
│   │       │   │   ├── IPropertyAggregateRepository.cs
│   │       │   │   ├── IPlotAggregateRepository.cs
│   │       │   │   ├── ISensorAggregateRepository.cs
│   │       │   │   ├── IPropertyReadStore.cs           # Read-optimized queries
│   │       │   │   ├── IPlotReadStore.cs
│   │       │   │   └── ISensorReadStore.cs
│   │       │   └── Mappers/
│   │       │       └── IntegrationEventMapper.cs       # Domain → Integration Event mapping
│   │       └── DependencyInjection.cs
│   │
│   └── Adapters/                                       # Infrastructure & Presentation
│       ├── Inbound/                                    # 🌐 Presentation Layer (REST API)
│       │   └── TC.Agro.Farm.Service/
│       │       ├── Program.cs                          # Bootstrap + DI Container
│       │       ├── Endpoints/                          # 🚀 FastEndpoints
│       │       │   ├── Properties/                     # Property endpoints
│       │       │   ├── Plots/                          # Plot endpoints
│       │       │   ├── Sensors/                        # Sensor endpoints
│       │       │   └── Owners/                         # Owner endpoints
│       │       ├── Extensions/
│       │       │   └── ServiceCollectionExtensions.cs  # DI configuration
│       │       ├── Telemetry/
│       │       │   ├── FarmMetrics.cs                  # Prometheus metrics
│       │       │   └── ActivitySourceFactory.cs        # OpenTelemetry tracing
│       │       ├── appsettings.json
│       │       ├── appsettings.Development.json
│       │       └── appsettings.Production.json
│       │
│       └── Outbound/                                   # 🗄️ Infrastructure Layer
│           └── TC.Agro.Farm.Infrastructure/
│               ├── Repositories/                       # Data access implementations
│               │   ├── PropertyAggregateRepository.cs
│               │   ├── PlotAggregateRepository.cs
│               │   ├── SensorAggregateRepository.cs
│               │   ├── PropertyReadStore.cs            # Read-optimized queries
│               │   ├── PlotReadStore.cs
│               │   └── SensorReadStore.cs
│               ├── Persistence/
│               │   ├── ApplicationDbContext.cs         # EF Core DbContext
│               │   └── Configurations/                 # Entity configurations
│               │       ├── PropertyAggregateConfiguration.cs
│               │       ├── PlotAggregateConfiguration.cs
│               │       ├── SensorAggregateConfiguration.cs
│               │       └── OwnerSnapshotConfiguration.cs
│               ├── Migrations/                         # EF Core migrations
│               └── DependencyInjection.cs
│
├── test/
│   └── TC.Agro.Farm.Tests/                            # 🧪 Unit & Integration Tests
│       ├── Domain/
│       │   ├── Aggregates/
│       │   │   ├── PropertyAggregateTests.cs
│       │   │   ├── PlotAggregateTests.cs
│       │   │   └── SensorAggregateTests.cs
│       │   └── ValueObjects/
│       │       ├── PropertyNameTests.cs
│       │       ├── AddressTests.cs
│       │       └── CoordinatesTests.cs
│       ├── Application/
│       │   ├── UseCases/
│       │   │   ├── Properties/CreatePropertyCommandHandlerTests.cs
│       │   │   ├── Plots/CreatePlotCommandHandlerTests.cs
│       │   │   └── Sensors/CreateSensorCommandHandlerTests.cs
│       │   └── MessageBrokerHandlers/
│       │       └── OwnerSnapshotHandlerTests.cs
│       ├── Infrastructure/
│       │   └── Repositories/
│       │       └── PropertyAggregateRepositoryTests.cs (integration)
│       └── Builders/                                   # Test data builders
│           ├── PropertyAggregateBuilder.cs
│           ├── PlotAggregateBuilder.cs
│           └── SensorAggregateBuilder.cs
│
├── docs/                                               # 📚 Technical documentation
│   └── ARCHITECTURE.md                                 # Architecture guide
│
├── docker-compose.yml                                  # Local stack (PostgreSQL + RabbitMQ + Redis)
├── Dockerfile                                          # Production container
├── Directory.Packages.props                            # Central Package Management (CPM)
├── .editorconfig                                       # Code style
└── README.md
```

### Layers and Responsibilities

| Layer | Responsibility | Dependencies |
|-------|----------------|--------------|  
| **Domain** | Business rules, aggregates, value objects, domain events | None (pure domain logic) |
| **Application** | Use cases (commands/queries), message handlers, interfaces | Domain |
| **Infrastructure** | Persistence (EF Core), messaging (Wolverine), integrations | Application, Domain |
| **Presentation** | REST API (FastEndpoints), health checks, telemetry | Application |

### Key Architectural Patterns

- ✅ **Clean Architecture** - Separation of concerns in layers (Domain → Application → Infrastructure → Presentation)
- ✅ **Domain-Driven Design (DDD)** - Rich domain modeling with Aggregates and Value Objects
- ✅ **CQRS** - Separation of commands (write) and queries (read) for optimal performance
- ✅ **Event-Driven Architecture** - Asynchronous communication via RabbitMQ/Wolverine
- ✅ **Outbox Pattern** - Transactional consistency of messages (Wolverine Outbox)
- ✅ **Repository Pattern** - Persistence abstraction with separate read stores for queries
- ✅ **Result Pattern** - Type-safe error handling without exceptions (Ardalis.Result)
- ✅ **Snapshot Pattern** - Denormalized data cache (OwnerSnapshot) for query optimization

---

## 🧪 Running Tests

### Run All Tests

```bash
# Complete test suite
dotnet test

# With detailed output
dotnet test --verbosity normal

# Tests from a specific category
dotnet test --filter "FullyQualifiedName~Domain"
dotnet test --filter "FullyQualifiedName~Application"
dotnet test --filter "FullyQualifiedName~Infrastructure"
```

### Run with Code Coverage

```bash
# Collect coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report (requires ReportGenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open report
start coveragereport/index.html  # Windows
open coveragereport/index.html   # Mac/Linux
```

### Test Structure

```
test/TC.Agro.Farm.Tests/
├── Domain/                        # Domain tests (pure, no mocks)
│   ├── Aggregates/
│   │   └── PropertyAggregateTests.cs
│   └── ValueObjects/
│       └── AddressTests.cs
├── Application/                   # Application tests (with mocks)
│   ├── UseCases/
│   └── MessageBrokerHandlers/
└── Infrastructure/                # Integration tests (database)
    └── Repositories/
        └── PropertyAggregateRepositoryTests.cs
```

### Tests in Watch Mode

```bash
# Run tests automatically on file save
dotnet watch test --project test/TC.Agro.Farm.Tests
```

---

## 🐳 Docker

### Build Image

```bash
docker build -t tc-agro-farm-service .
```

### Run Container

```bash
docker run -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=farm_db;Username=postgres;Password=yourpassword" \
  tc-agro-farm-service
```

---

## 🏥 Health Checks

The service exposes health check endpoints:

- `/health` - Basic health check
- `/health/ready` - Readiness check (includes database connectivity)
- `/health/live` - Liveness check

---

## 📊 Observability

### Logging

Logs are structured using Serilog and can be exported to:
- Console
- Grafana Loki
- OpenTelemetry Collector

### Metrics & Tracing

OpenTelemetry is configured for:
- Distributed tracing
- Prometheus metrics (available at `/metrics`)

---

## 📚 Documentation

For more detailed information, refer to the following documentation:

- **[Architecture Guide](docs/ARCHITECTURE.md)** - Detailed architectural decisions, patterns, and design principles
- **[API Reference](https://localhost:5001/swagger)** - Interactive API documentation (Swagger UI)
- **Database Schema** - EF Core migrations in `src/Adapters/Outbound/TC.Agro.Farm.Infrastructure/Migrations/`

### Related Services Documentation

- **[Analytics Worker](../analytics-worker/README.md)** - Consumes Farm Service events for alert detection
- **[Sensor Ingest Service](../sensor-ingest/README.md)** - Consumes sensor lifecycle events
- **[Identity Service](../identity-service/README.md)** - Provides user/owner data via events

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
