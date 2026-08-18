# Software Architecture

This document describes the software architecture of the Ontology Service.

## Overview

The Ontology Service consists of a backend service which is a .NET 8 application that provides a REST API for uploading a new ontology file.

## Backend Architecture
The backend is a modular, multi-project solution and consists of the following layers:

- **API Layer (`Ontology.Api`)**:  
  This layer is responsible for handling incoming HTTP requests and sending responses. It contains controllers, configuration (options), service components. Additionally, it includes program entry points and Docker configurations for deployment. Extensions are used for modularity and shared functionality.
- **Domain Layer (`Ontology.Domain`)**:  
  The domain layer encapsulates the core business models and repository interfaces of the application. This layer defines the fundamental building blocks and rules of the domain, independent of technical concerns.
- **Events Layer (`Ontology.Events`)**:  
  This layer is responsible for defining application events and event-related models, such as `UploadedOntologyFile`. It acts as a contract for events published and consumed within the system.
- **Infrastructure Layer (`Ontology.Infrastructure`)**:  
  This layer contains the concrete implementations of repositories and database migrations required for data persistence. It provides the database context and documentation for migration operations.
- **Test Projects (`Ontology.Infrastructure.Tests`)**:  
  This project includes unit and integration tests for the infrastructure layer, ensuring the correctness of repositories.
- **Build Project (`_build`)**:  
  This project is dedicated to the build and automation scripts used for CI/CD.