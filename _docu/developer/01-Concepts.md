# Concepts

This document describes the main concepts used in the Ontology Service.

## Ontology Management

The `Ontology Service` is responsible for uploading an ontology file.

### Ontology

The Ontology is a ttl or rdf file which provides rules for nodes and their relationships.

## Authentication and Authorization

Authentication and authorization are handled by Keycloak. Before requesting an upload to the `Ontology Service`, a client must authenticate. Authentication can be enabled by setting the `ASPNETCORE_ENVIRONMENT` to any value other than `Development`.

## Event Driven Design with Kafka

The `Ontology Service` uses an event-driven architecture to communicate Ontology changes across the system. This is implemented using [Apache Kafka](https://kafka.apache.org/) as the message broker.

### Kafka Events

Whenever a new Ontology is uploaded, a corresponding event is published to Kafka. This event allows other services to subscribe to Ontology changes, promoting loose coupling and enabling real-time reactions elsewhere in the platform.