// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using OntologyService.Domain.Models;

namespace OntologyService.Domain.Repositories;

/// <summary>
/// Represents a repository for managing outbox events.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a new event to the outbox for later processing.
    /// </summary>
    /// <param name="evt">The event object to add.</param>
    /// <param name="topic">The Kafka topic associated with the event.</param>
    /// <param name="aggregateId">The aggregate identifier related to the event.</param>
    void Add(object evt, string topic, string aggregateId);

    /// <summary>
    /// Retrieves a batch of unprocessed outbox events.
    /// </summary>
    /// <param name="batchSize">The maximum number of events to retrieve.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A list of unprocessed <see cref="OutboxEvent"/> objects.</returns>
    Task<List<OutboxEvent>> GetUnprocessedAsync(int batchSize, CancellationToken ct);

    /// <summary>
    /// Removes the specified event from the outbox.
    /// </summary>
    /// <param name="evt">The outbox event to remove.</param>
    void Remove(OutboxEvent evt);

    /// <summary>
    /// Increments the retry count for the specified outbox event.
    /// </summary>
    /// <param name="evt">The outbox event for which to increment the retry count.</param>
    void IncrementRetry(OutboxEvent evt);

    /// <summary>
    /// Saves all changes made in the repository to the database.
    /// </summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveChangesAsync(CancellationToken ct);
}