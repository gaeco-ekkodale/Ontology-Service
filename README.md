<div align="center">
  <img src="https://raw.githubusercontent.com/gaeco-ekkodale/.github/main/assets/gaeco_logo_horizontal_color.png" width="200" alt="gaeco logo">

  # OntologyService

  <em>Manages the gaeco ontology (RDF/OWL/Turtle) that defines which relationships between classifications are allowed.</em>

  [![License](https://img.shields.io/badge/license-fair--code-blue.svg)](LICENSE.md)
  [![Version](https://img.shields.io/github/v/release/gaeco-ekkodale/Ontology-Service)](../../releases)

  [gaeco-ekkodale Organization](https://github.com/gaeco-ekkodale) · [All Repos](https://github.com/orgs/gaeco-ekkodale/repositories)
</div>

---

gaeco (Graphs for Architecture, Engineering, Construction, Operations) is an event-driven microservice platform for BIM data management. It translates external building-industry standards (IFC, IBPDI, Brick Schema, ASHRAE 223 and others) into a shared, versioned classification and relationship model (Guideline + Ontology) and exposes consistent, graph-based building data (Instance) across use cases and departments — without forcing every consumer onto one rigid schema. Built for organizations managing building/portfolio data across disconnected departmental systems (construction, facilities management, leasing, accounting) that need automatic, reliable data propagation instead of manual, error-prone hand-offs.

> This project is licensed under the [Source Available](LICENSE.md). Source code is viewable and usable; commercial use is restricted.

---

## What this service does

The OntologyService owns the relationship half of gaeco's semantic model. It accepts an ontology file — a `.ttl` or `.rdf` document — stores it, and makes it available to the rest of the platform.

Where the [GuidelineService](https://github.com/gaeco-ekkodale/GuidelineService) defines *what* things are (classifications and their properties), the ontology defines *how they may be connected*: which node types can be related to which others, and through which predicates. The [InstanceService](https://github.com/gaeco-ekkodale/InstanceService) validates every subject–predicate–object triple in the building data graph against these rules.

This is a server-only service; ontologies are uploaded through the [PlatformConfig](https://github.com/gaeco-ekkodale/PlatformConfig) admin UI or directly via the API.

## Repository Structure

- `Server/Api/`: ASP.NET Core Web API
- `Server/Domain/`: domain models and contracts
- `Server/Infrastructure/`: EF Core data access, MinIO and Kafka integration
- `Server/Events/`: Kafka event contracts
- `Server/Api.Tests/`, `Server/Infrastructure.Tests/`: unit tests
- `_docker/`: Compose definition, env schemas and the App Registry package manifest
- `_docu/`: developer and user documentation
- `_pipeline/`: Azure DevOps CI/CD pipeline definitions
- `build/`: NUKE build scripts

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core, Swagger/Swashbuckle, OpenTelemetry
- **Infrastructure**: PostgreSQL, MinIO (ontology file storage), Apache Kafka, Keycloak, Docker
- **Build**: NUKE

## Local Development

### Prerequisites

- Docker Desktop
- .NET 8 SDK
- The shared platform infrastructure (Keycloak, MinIO, Kafka) — see [`_docu/user/01-Installation.md`](_docu/user/01-Installation.md)

### Start with Docker Compose

```bash
cd _docker
docker compose -p ontology-service -f docker-compose.yml -f docker-compose-override.yml up -d
```

Ports are driven by the `ONTOLOGY_*_OUTERPORT` variables in the environment files; the API exposes Swagger at `/swagger`.

## Build and Test

```bash
./build.sh     # Linux/macOS
.\build.ps1    # Windows
```

Backend tests: `dotnet test` from the repository root.

## Integration

- **Authentication**: Keycloak (OIDC/JWT). A client must authenticate before uploading. Authentication is active whenever `ASPNETCORE_ENVIRONMENT` is not `Development`.
- **Events**: every ontology upload publishes an event to Apache Kafka, so subscribing services pick up the new relationship rules without synchronous calls.
- **Storage**: the ontology file itself lives in MinIO; metadata is kept in PostgreSQL.

## Documentation

- [Concepts](_docu/developer/01-Concepts.md)
- [Patterns](_docu/developer/02-Patterns.md)
- [Used Technologies](_docu/developer/03-Used-Technologies.md)
- [Data Model](_docu/developer/04-Data-Model.md)
- [Software Architecture](_docu/developer/05-Software-Architecture.md)
- [How To Ontology](_docu/How_To_Ontology.md) · [VSCode Extensions](_docu/VSCode_Extensions.md)
- [Installation](_docu/user/01-Installation.md) · [User Manual](_docu/user/02-User-Manual.md)
