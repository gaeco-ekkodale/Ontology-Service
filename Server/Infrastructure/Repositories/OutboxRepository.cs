// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.EntityFrameworkCore;
using OntologyService.Domain.Models;
using OntologyService.Domain.Repositories;
using System.Text.Json;

namespace OntologyService.Infrastructure.Repositories;

/// <summary>
/// Implements the repository for managing outbox events using an Entity Framework Core context.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly OntologyDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxRepository"/> class.
    /// </summary>
    /// <param name="context">The database context to be used for data operations.</param>
    public OutboxRepository(OntologyDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void Add(object evt, string topic, string aggregateId)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        var serialized = JsonSerializer.Serialize(evt);
        string? payload = serialized == "{}" ? null : serialized;

        var eventType = evt?.GetType().Name ?? string.Empty;

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be null or empty", nameof(evt));

        var outbox = new OutboxEvent(Guid.NewGuid(), topic, aggregateId, eventType, DateTimeOffset.UtcNow, payload);
        _context.OutboxEvents.Add(outbox);
    }

    /// <inheritdoc />
    public async Task<List<OutboxEvent>> GetUnprocessedAsync(int batchSize, CancellationToken ct)
    {
        return await _context.OutboxEvents
            .OrderBy(e => e.OccurredOn)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public void Remove(OutboxEvent evt)
    {
        _context.OutboxEvents.Remove(evt);
    }

    /// <inheritdoc />
    public void IncrementRetry(OutboxEvent evt)
    {
        evt.RetryCount++;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
