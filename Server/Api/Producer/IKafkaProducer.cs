// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace OntologyService.Api.Producer;

/// <summary>
/// Defines a contract for a Kafka producer, responsible for sending messages to Kafka topics.
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Asynchronously produces a message to the specified Kafka topic.
    /// </summary>
    /// <param name="topic">The name of the Kafka topic to which the message will be sent.</param>
    /// <param name="key">The key of the message, used for partitioning.</param>
    /// <param name="value">The value (payload) of the message.</param>
    /// <param name="headers">Optional headers to include with the message.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous produce operation.</returns>
    Task ProduceAsync(string topic, string key, string? value, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
}