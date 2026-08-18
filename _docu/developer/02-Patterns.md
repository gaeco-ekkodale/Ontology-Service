# Patterns

This document describes the design patterns used in the Ontology Service.

## Repository Pattern

The repository pattern is used in the backend to abstract the data access layer. The `Ontology.Server/Domain/Repositories/IOntologyRepository` interface defines the methods for accessing the data, and the `Ontology.Server/Infrastructure/Repositories/OntologyRepository` class provides the implementation. This pattern allows to easily switch the database implementation without changing the business logic.

## Options Pattern

The options pattern is used to configure the application. The `KafkaOptions`, `KeycloakOption`, `MinioOptions` and `PostgresOption` classes define the configuration options, and the `appsettings.json` file provides the values. This pattern allows to change the configuration without recompiling the application.

## Outbox Pattern

The outbox pattern is implemented to ensure reliable event publishing when a new Ontology is uploaded. This implementation guarantees reliable delivery and error handling by supporting automatic retries.