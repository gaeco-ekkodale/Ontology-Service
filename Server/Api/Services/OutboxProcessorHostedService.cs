// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using OntologyService.Api.Producer;
using OntologyService.Domain.Repositories;

namespace OntologyService.Api.Services;

/// <summary>
/// Represents a background service that processes and publishes outbox events to Kafka.
/// </summary>
/// <remarks>
/// This service periodically queries the outbox for unprocessed events, attempts to publish them,
/// and manages their state (e.g., removing on success, incrementing retry count on failure).
/// </remarks>
public class OutboxProcessorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessorHostedService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The factory for creating service scopes.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    public OutboxProcessorHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the main processing loop for the outbox service.
    /// </summary>
    /// <remarks>
    /// This method runs continuously, polling the outbox repository for unprocessed events
    /// at regular intervals and publishing them to the configured Kafka topic.
    /// </remarks>
    /// <param name="stoppingToken">A token that is triggered when the application host is stopping.</param>
    /// <returns>A <see cref="Task"/> that represents the long-running outbox processing operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();
                var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                var events = await outboxRepo.GetUnprocessedAsync(50, stoppingToken);

                foreach (var evt in events)
                {
                    try
                    {
                        var headers = new Dictionary<string, string>
                        {
                            {"event_type", evt.EventType},
                            {"occurred_on", evt.OccurredOn.ToString("O")}
                        };
                        var topic = evt.Topic;
                        await producer.ProduceAsync(topic, evt.AggregateId, evt.Payload, headers, stoppingToken);
                        outboxRepo.Remove(evt);
                    }
                    catch (Exception ex)
                    {
                        outboxRepo.IncrementRetry(evt);
                        _logger.LogError(ex, "Failed to publish outbox event {EventId}", evt.Id);
                    }
                }

                if (events.Count > 0)
                {
                    await outboxRepo.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        _logger.LogInformation("Outbox processor stopped");
    }
}
