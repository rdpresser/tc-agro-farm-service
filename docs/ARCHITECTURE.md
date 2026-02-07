# TC.Agro Farm Service - Architecture Documentation

## Overview

The **Farm Service** is a microservice responsible for managing agricultural properties, plots, and sensors. It is part of the TC.Agro Solutions platform and follows Clean Architecture (Hexagonal Architecture) principles.

## Architecture Style

The service implements **Hexagonal Architecture** (Ports and Adapters), ensuring:

- **Domain-centric design**: Business logic is isolated in the core
- **Dependency inversion**: External concerns depend on abstractions defined in the core
- **Testability**: Easy to test business logic in isolation
- **Flexibility**: Easy to swap infrastructure implementations

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Inbound Adapters                              │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              TC.Agro.Farm.Service (API)                      │    │
│  │    FastEndpoints │ Authentication │ OpenTelemetry            │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                             CORE                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              TC.Agro.Farm.Application                        │    │
│  │    Use Cases │ Commands/Queries │ Handlers │ Ports           │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                │                                     │
│                                ▼                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                TC.Agro.Farm.Domain                           │    │
│  │    Aggregates │ Value Objects │ Domain Events │ Errors       │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       Outbound Adapters                              │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │             TC.Agro.Farm.Infrastructure                      │    │
│  │    EF Core │ PostgreSQL │ Wolverine │ Repositories           │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
src/
├── Adapters/
│   ├── Inbound/
│   │   └── TC.Agro.Farm.Service/        # API Layer (REST Endpoints)
│   │       ├── Endpoints/                # FastEndpoints implementations
│   │       │   ├── Properties/
│   │       │   ├── Plots/
│   │       │   └── Sensors/
│   │       ├── Extensions/               # Service configurations
│   │       ├── Telemetry/                # OpenTelemetry setup
│   │       └── Program.cs                # Application entry point
│   │
│   └── Outbound/
│       └── TC.Agro.Farm.Infrastructure/  # Infrastructure Layer
│           ├── Configurations/           # EF Core entity configs
│           ├── Messaging/                # Wolverine messaging
│           ├── Migrations/               # Database migrations
│           └── Repositories/             # Repository implementations
│
└── Core/
    ├── TC.Agro.Farm.Application/         # Application Layer
    │   ├── Abstractions/
    │   │   ├── Mappers/                  # DTO mappers
    │   │   └── Ports/                    # Repository interfaces
    │   └── UseCases/
    │       ├── Properties/               # Property use cases
    │       ├── Plots/                    # Plot use cases
    │       └── Sensors/                  # Sensor use cases
    │
    └── TC.Agro.Farm.Domain/              # Domain Layer
        ├── Abstractions/                 # Base domain types
        ├── Aggregates/                   # Aggregate roots
        └── ValueObjects/                 # Value objects

test/
└── TC.Agro.Farm.Tests/                   # Unit & Integration Tests
    └── Domain/
        ├── Aggregates/
        └── ValueObjects/
```

## Domain Model

### Aggregates

#### PropertyAggregate
Represents a farm property owned by a producer.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Name` | `Name` | Property name |
| `Location` | `Location` | Address and coordinates |
| `AreaHectares` | `Area` | Total area in hectares |
| `OwnerId` | `Guid` | Owner (producer) identifier |
| `IsActive` | `bool` | Active/inactive status |

**Commands**: Create, Update, Activate, Deactivate

#### PlotAggregate
Represents a plot (talhão) within a property.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `PropertyId` | `Guid` | Parent property reference |
| `Name` | `Name` | Plot name |
| `CropType` | `CropType` | Current crop type |
| `AreaHectares` | `Area` | Plot area in hectares |
| `IsActive` | `bool` | Active/inactive status |

**Commands**: Create, Update, ChangeCropType, Activate, Deactivate

#### SensorAggregate
Represents a sensor installed in a plot.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `PlotId` | `Guid` | Parent plot reference |
| `Type` | `SensorType` | Sensor type |
| `Status` | `SensorStatus` | Current status |
| `Label` | `Name?` | Optional label |
| `InstalledAt` | `DateTimeOffset` | Installation timestamp |

**Commands**: Create, UpdateLabel, SetActive, SetInactive, SetMaintenance, SetFaulty

### Value Objects

| Value Object | Description |
|--------------|-------------|
| `Name` | Validated string for names (2-255 characters) |
| `Area` | Positive area measurement in hectares |
| `Location` | Address with optional GPS coordinates |
| `CropType` | Enumerated crop types |
| `SensorType` | Enumerated sensor types (Temperature, Humidity, SoilMoisture, etc.) |
| `SensorStatus` | Sensor operational status (Active, Inactive, Maintenance, Faulty) |

## Application Layer

### Use Case Pattern

Each use case follows a consistent structure:

```
UseCases/
└── {Entity}/
    └── {Action}/
        ├── {Action}Command.cs           # Command record (request)
        ├── {Action}CommandHandler.cs    # Business logic handler
        ├── {Action}CommandValidator.cs  # FluentValidation rules
        ├── {Action}Mapper.cs            # Mapping utilities
        └── {Action}Response.cs          # Response DTO
```

### Command Flow

```
1. API Endpoint receives HTTP request
         │
         ▼
2. Command is created and validated (FluentValidation)
         │
         ▼
3. Handler executes business logic:
   a. Map command to aggregate
   b. Validate business rules
   c. Persist aggregate
   d. Publish integration events
   e. Return response
         │
         ▼
4. Response is serialized and returned
```

### Ports (Interfaces)

**Write Repositories** (Command side):
- `IPropertyAggregateRepository`
- `IPlotAggregateRepository`
- `ISensorAggregateRepository`

**Read Stores** (Query side):
- `IPropertyReadStore`
- `IPlotReadStore`
- `ISensorReadStore`

## Infrastructure Layer

### Database

- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10
- **Schema**: `farm`
- **Naming Convention**: snake_case

### Messaging

- **Framework**: Wolverine
- **Pattern**: Transactional Outbox
- **Purpose**: Reliable integration event publishing

### Caching

- **Library**: ZiggyCreatures.FusionCache
- **Backend**: Redis (distributed) + Memory (L1)
- **Strategy**: Cache-aside pattern

## API Layer

### Framework

- **FastEndpoints**: Minimal API framework
- **Authentication**: JWT Bearer tokens
- **Authorization**: Role-based (Admin, Producer)

### Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/property` | Create property |
| `GET` | `/api/property/{id}` | Get property by ID |
| `GET` | `/api/property` | List properties |
| `PUT` | `/api/property/{id}` | Update property |
| `POST` | `/api/plot` | Create plot |
| `GET` | `/api/plot/{id}` | Get plot by ID |
| `GET` | `/api/plot` | List plots |
| `PUT` | `/api/plot/{id}` | Update plot |
| `POST` | `/api/sensor` | Register sensor |
| `GET` | `/api/sensor/{id}` | Get sensor by ID |
| `GET` | `/api/sensor` | List sensors |
| `PUT` | `/api/sensor/{id}` | Update sensor |

## Observability

### Logging
- **Framework**: Serilog
- **Sinks**: Console, Grafana Loki, OpenTelemetry
- **Enrichers**: Sensitive data masking, Span context

### Tracing & Metrics
- **Framework**: OpenTelemetry
- **Instrumentation**: ASP.NET Core, HTTP, EF Core, Redis, FusionCache
- **Exporters**: OTLP, Prometheus

## Technology Stack

| Category | Technology |
|----------|------------|
| Runtime | .NET 10 |
| API Framework | FastEndpoints 7.2 |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL |
| Caching | Redis + FusionCache |
| Messaging | Wolverine |
| Logging | Serilog |
| Observability | OpenTelemetry |
| Validation | FluentValidation |
| Documentation | Swagger/OpenAPI |
| Containerization | Docker |

## Design Patterns

| Pattern | Usage |
|---------|-------|
| **Aggregate Root** | Domain entities with consistency boundaries |
| **Value Object** | Immutable domain concepts |
| **Domain Events** | Internal domain state changes |
| **Integration Events** | Cross-service communication |
| **Repository** | Data access abstraction |
| **CQRS (Light)** | Separate read/write models |
| **Transactional Outbox** | Reliable event publishing |
| **Result Pattern** | Explicit error handling |
| **Factory Method** | Aggregate creation with validation |

## Event-Driven Communication

### Domain Events (Internal)
- `PropertyCreatedDomainEvent`
- `PropertyUpdatedDomainEvent`
- `PropertyActivatedDomainEvent`
- `PropertyDeactivatedDomainEvent`
- `PlotCreatedDomainEvent`
- `PlotUpdatedDomainEvent`
- `PlotCropTypeChangedDomainEvent`
- `SensorRegisteredDomainEvent`
- `SensorStatusChangedDomainEvent`
- `...`

### Integration Events (Cross-service)
Published via Wolverine to message broker for consumption by other services.

## Security

- **Authentication**: JWT Bearer tokens via external Identity Provider
- **Authorization**: Role-based access control (RBAC)
- **Roles**: Admin, Producer
- **Data Isolation**: Owner-based data filtering
- **Sensitive Data**: Masked in logs

## Configuration

Configuration follows the standard ASP.NET Core pattern:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- Environment variables - Production/container overrides

Key configuration sections:
- `ConnectionStrings` - Database and Redis connections
- `Serilog` - Logging configuration
- `OpenTelemetry` - Observability settings
- `Cors` - Cross-origin resource sharing
