# TC.Agro Farm Service

> Microservice for managing agricultural properties, plots, and sensors in the TC.Agro Solutions platform.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=flat&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Overview

The **Farm Service** is responsible for managing the core farm resources:

- **Properties** - Farm properties owned by producers
- **Plots** - Agricultural plots (talhões) within properties
- **Sensors** - IoT sensors installed in plots for monitoring

## Architecture

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

## Technology Stack

| Category | Technology |
|----------|------------|
| Runtime | .NET 10 |
| API | FastEndpoints |
| Database | PostgreSQL + EF Core |
| Caching | Redis + FusionCache |
| Messaging | Wolverine |
| Observability | OpenTelemetry, Serilog |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [Redis](https://redis.io/) (optional, for distributed caching)
- [Docker](https://www.docker.com/) (optional, for containerized deployment)

## Getting Started

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

## API Endpoints

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

## Project Structure

```
src/
├── Adapters/
│   ├── Inbound/
│   │   └── TC.Agro.Farm.Service/     # REST API
│   └── Outbound/
│       └── TC.Agro.Farm.Infrastructure/  # Database & Messaging
└── Core/
    ├── TC.Agro.Farm.Application/     # Use Cases
    └── TC.Agro.Farm.Domain/          # Business Logic

test/
└── TC.Agro.Farm.Tests/               # Unit & Integration Tests
```

## Running Tests

```bash
dotnet test
```

## Docker

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

## Health Checks

The service exposes health check endpoints:

- `/health` - Basic health check
- `/health/ready` - Readiness check (includes database connectivity)
- `/health/live` - Liveness check

## Observability

### Logging

Logs are structured using Serilog and can be exported to:
- Console
- Grafana Loki
- OpenTelemetry Collector

### Metrics & Tracing

OpenTelemetry is configured for:
- Distributed tracing
- Prometheus metrics (available at `/metrics`)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
