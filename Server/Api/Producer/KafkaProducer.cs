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
using Microsoft.Extensions.Options;
using OntologyService.Api.Options;

namespace OntologyService.Api.Producer;

/// <summary>
/// A Kafka producer implementation for sending messages.
/// </summary>
public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaProducer"/> class.
    /// </summary>
    /// <param name="options">The Kafka configuration options.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.Address,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 5,
            RetryBackoffMs = 200,
            MessageMaxBytes = 10485760 // 10 MB – aligned with broker's message.max.bytes
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <inheritdoc />
    public async Task ProduceAsync(string topic, string key, string? value, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = new Message<string, string>
            {
                Key = key,
                // Value can be null to produce a tombstone (delete) record
                Value = value
            };
            if (headers != null && headers.Count > 0)
            {
                msg.Headers = new Headers();
                foreach (var h in headers)
                {
                    msg.Headers.Add(h.Key, System.Text.Encoding.UTF8.GetBytes(h.Value));
                }
            }
            var dr = await _producer.ProduceAsync(topic, msg, cancellationToken);
            _logger.LogInformation("Produced message to {Topic} partition {Partition} offset {Offset}", dr.Topic, dr.Partition, dr.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Error producing message to {Topic}", topic);
            throw;
        }
    }

    /// <summary>
    /// Flushes any buffered messages to Kafka and disposes the producer instance.
    /// </summary>
    public void Dispose()
    {
        try { _producer.Flush(TimeSpan.FromSeconds(5)); } catch { /* ignore */ }
        _producer.Dispose();
    }
}
