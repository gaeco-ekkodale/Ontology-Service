# Ontology Service

Der Ontology Service ist verantwortlich für die Verwaltung und Bereitstellung von Ontologien im Gaeco-Ökosystem. Er stellt eine API zur Verfügung, über die Ontologien abgerufen und aktualisiert werden können. Ontologien werden in MinIO gespeichert und über Kafka-Events an andere Dienste kommuniziert.

## Enthaltene Dienste

- **Ontology Server** (`ontology-server`) – .NET Backend, erreichbar über Traefik unter `ONTOLOGY_SERVER_HOSTNAME`
