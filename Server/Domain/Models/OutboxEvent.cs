// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OntologyService.Domain.Models;

/// <summary>
/// Represents an event stored in the outbox pattern for guaranteed message delivery.
/// </summary>
[Table("outbox_event")]
public class OutboxEvent
{
    /// <summary>
    /// Gets the unique identifier for the outbox event.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the aggregate root that this event belongs to.
    /// </summary>
    [Required]
    [MaxLength(40)]
    [Column("aggregate_id")]
    public string AggregateId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the topic name where the event will be published.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("topic")]
    public string Topic { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the type or name of the event.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("event_type")]
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    [Required]
    [Column("occurred_on")]
    public DateTimeOffset OccurredOn { get; private set; }

    /// <summary>
    /// Gets the JSON-serialized payload of the event. Can be null.
    /// </summary>
    [Column("payload")]
    public string? Payload { get; private set; }

    /// <summary>
    /// Gets or sets the number of times the processing of this event has been attempted.
    /// </summary>
    [Required]
    [Column("retry_count")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxEvent"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="topic">The topic name for publishing the event.</param>
    /// <param name="aggregateId">The identifier of the related aggregate.</param>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="occurredOn">The timestamp of when the event occurred.</param>
    /// <param name="payload">The event's data payload.</param>
    public OutboxEvent(Guid id, string topic, string aggregateId, string eventType, DateTimeOffset occurredOn, string? payload)
    {
        Id = id;
        Topic = topic;
        AggregateId = aggregateId;
        EventType = eventType;
        OccurredOn = occurredOn;
        Payload = payload;
        RetryCount = 0;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxEvent"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is required by Entity Framework Core for materialization.
    /// </remarks>
    private OutboxEvent() { }
}
