// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;

namespace OntologyService.Api.Options;

/// <summary>
/// Provides configuration options for Kafka.
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// The name of the configuration section for Kafka settings.
    /// </summary>
    public const string SectionName = "Kafka";

    /// <summary>
    /// Gets or sets the Kafka broker address.
    /// </summary>
    [Required]
    public required string Address { get; set; }

    /// <summary>
    /// Gets or sets the username for Kafka authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for Kafka authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the consumer group ID.
    /// </summary>
    public required string ConsumerGroup { get; set; }

    /// <summary>
    /// Gets or sets the auto offset reset policy.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Confluent.Kafka.AutoOffsetReset.Earliest"/>.
    /// </remarks>
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;

    /// <summary>
    /// Gets or sets the session timeout in milliseconds.
    /// </summary>
    /// <remarks>
    /// Defaults to 150000 ms.
    /// </remarks>
    public int SessionTimeoutMs { get; set; } = 150000;

    /// <summary>
    /// Gets or sets the maximum poll interval in milliseconds.
    /// </summary>
    /// <remarks>
    /// Defaults to 150000 ms.
    /// </remarks>
    public int MaxPollIntervalMs { get; set; } = 150000;

    /// <summary>
    /// Gets or sets the Kafka topic configurations.
    /// </summary>
    [Required]
    public required KafkaTopicsOptions Topics { get; set; }
}

/// <summary>
/// Provides configuration options for Kafka topic names.
/// </summary>
public class KafkaTopicsOptions
{
    /// <summary>
    /// Gets or sets the name of the ontology topic.
    /// </summary>
    [Required]
    public required string Ontology { get; set; }
}
