// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OntologyService.Infrastructure.Repositories;

namespace OntologyService.Infrastructure.Tests.Repositories;

public class OutboxRepositoryTests : IDisposable
{
    private readonly Faker _faker = new();
    private readonly OntologyDbContext _context;
    private readonly OutboxRepository _repo;

    public OutboxRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OntologyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new OntologyDbContext(options);
        _repo = new OutboxRepository(_context);
    }

    [Fact]
    public async Task When_AddingEvent_Then_EventAddedWithDefaults()
    {
        _repo.Add(new { Something = "Value" }, "test-topic", _faker.Random.AlphaNumeric(10));
        await _repo.SaveChangesAsync(CancellationToken.None);

        var evt = await _context.OutboxEvents.FirstAsync();
        evt.Id.Should().NotBeEmpty();
        evt.EventType.Should().NotBeNullOrWhiteSpace();
        evt.Payload.Should().Contain("Value");
        evt.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task When_GettingUnprocessedEvents_Then_ReturnsAllEventsOrderedLimited()
    {
        // Add 4 events
        for (int i = 0; i < 4; i++)
        {
            _repo.Add(new { Index = i }, "test-topic", _faker.Random.AlphaNumeric(8));
        }
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Remove one event (simulate processing)
        var toRemove = await _context.OutboxEvents.OrderBy(e => e.OccurredOn).FirstAsync();
        _repo.Remove(toRemove);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var list = await _repo.GetUnprocessedAsync(2, CancellationToken.None);

        list.Should().HaveCount(2);
        // Ensure ordering by OccurredOn (ascending)
        list.Select(e => e.OccurredOn).Should().BeInAscendingOrder();

        // Verify total remaining events in database
        _context.OutboxEvents.Should().HaveCount(3);
    }

    [Fact]
    public async Task When_RemovingEvent_Then_EventRemovedFromDatabase()
    {
        _repo.Add(new { Foo = "Bar" }, "test-topic", _faker.Random.AlphaNumeric(8));
        await _repo.SaveChangesAsync(CancellationToken.None);
        var evt = await _context.OutboxEvents.FirstAsync();

        _repo.Remove(evt);
        await _repo.SaveChangesAsync(CancellationToken.None);

        _context.OutboxEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task When_IncrementingRetry_Then_RetryCountIncremented()
    {
        _repo.Add(new { Foo = "Bar" }, "test-topic", _faker.Random.AlphaNumeric(8));
        await _repo.SaveChangesAsync(CancellationToken.None);
        var evt = await _context.OutboxEvents.FirstAsync();

        _repo.IncrementRetry(evt);
        _repo.IncrementRetry(evt);
        await _repo.SaveChangesAsync(CancellationToken.None);

        evt.RetryCount.Should().Be(2);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}