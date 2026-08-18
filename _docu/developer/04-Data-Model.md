# Data Model
This document describes the data model of the Ontology Service.

## OutboxEvent

The `OutboxEvent` represents a domain-specific event that is persisted for later processing as part of the outbox pattern. It has two constructors, one for EF Core and a public one. The following properties are contained:

- **Id** (`Guid`): The unique identifier for the outbox event.
- **AggregateId** (`string`, max. 40 characters): The identifier for the aggregate root (domain entity) that this event is associated with.
- **Topic** (`string`, max. 200 characters): The name of the kafka topic to which this event will be published.
- **EventType** (`string`, max. 200 characters): The type or name of the event. This distinguishes between different event kinds.
- **OccurredOn** (`DateTimeOffset`): The timestamp capturing when the event happened.
- **Payload** (`string?`): An optional JSON-serialized payload containing the event's data. Can be `null` if the event requires no additional data.
- **RetryCount** (`int`): The number of times delivery or processing for this event has been attempted. This starts at `0` when a new event is created and is incremented on each processing attempt.