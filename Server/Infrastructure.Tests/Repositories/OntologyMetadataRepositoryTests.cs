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
using OntologyService.Infrastructure.Repositories;

namespace OntologyService.Infrastructure.Tests.Repositories;

public class OntologyMetadataRepositoryTests : IDisposable
{
    private readonly OntologyDbContext _context;
    private readonly OntologyMetadataRepository _repo;

    public OntologyMetadataRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OntologyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new OntologyDbContext(options);
        _repo = new OntologyMetadataRepository(_context);
    }

    [Fact]
    public void Add_WithValidOntology_StagesForInsertion()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(ontology);

        Assert.Contains(ontology, _context.Ontologies.Local);
    }

    [Fact]
    public void Add_WithNullOntology_ThrowsArgumentNullException()
    {
        var action = () => _repo.Add(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleOntologies_ReturnsOrderedByCreatedAtDescending()
    {
        var now = DateTimeOffset.UtcNow;
        var ontology1 = new Ontology(Guid.NewGuid(), "name1", "name1.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id1.ttl", "etag1", 100, now.AddHours(-2));
        var ontology2 = new Ontology(Guid.NewGuid(), "name2", "name2.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id2.ttl", "etag2", 100, now.AddHours(-1));
        var ontology3 = new Ontology(Guid.NewGuid(), "name3", "name3.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id3.ttl", "etag3", 100, now);

        _repo.Add(ontology1);
        _repo.Add(ontology2);
        _repo.Add(ontology3);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(ontology3.Id, result[0].Id);
        Assert.Equal(ontology2.Id, result[1].Id);
        Assert.Equal(ontology1.Id, result[2].Id);
    }

    [Fact]
    public async Task GetAllAsync_WithNoOntologies_ReturnsEmptyList()
    {
        var result = await _repo.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsOntology()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(ontology);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("name", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void Remove_WithValidOntology_StagesForDeletion()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(ontology);
        _repo.Remove(ontology);

        Assert.DoesNotContain(ontology, _context.Ontologies.Local);
    }

    [Fact]
    public void Remove_WithNullOntology_ThrowsArgumentNullException()
    {
        var action = () => _repo.Remove(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithAddedOntology_PersistsToDatabase()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(ontology);
        var changes = await _repo.SaveChangesAsync();

        Assert.Equal(1, changes);

        var result = await _repo.GetByIdAsync(id);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithRemovedOntology_DeletesFromDatabase()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(ontology);
        await _repo.SaveChangesAsync();

        _repo.Remove(ontology);
        var changes = await _repo.SaveChangesAsync();

        Assert.Equal(1, changes);

        var result = await _repo.GetByIdAsync(id);
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        var changes = await _repo.SaveChangesAsync();

        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task SaveChangesAsync_RespectsCancellationToken()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(ontology);
        var ct = CancellationToken.None;

        var changes = await _repo.SaveChangesAsync(ct);

        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task ReplaceFile_UpdatesAllFileRelatedFields()
    {
        var id = Guid.NewGuid();
        var originalOntology = new Ontology(id, "original", "original.ttl", "ttl", "text/turtle",
            "ontology", "ttl/original.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(originalOntology);
        await _repo.SaveChangesAsync();

        var retrieved = await _repo.GetByIdAsync(id);
        var newTime = DateTimeOffset.UtcNow.AddMinutes(1);
        retrieved!.ReplaceFile("newname", "newname.rdf", "rdf", "application/rdf+xml",
            "rdf/newname.rdf", "etag2", 200, newTime);

        await _repo.SaveChangesAsync();

        var updated = await _repo.GetByIdAsync(id);
        Assert.Equal("newname", updated!.Name);
        Assert.Equal("newname.rdf", updated.FileName);
        Assert.Equal("rdf", updated.Format);
        Assert.Equal("application/rdf+xml", updated.ContentType);
        Assert.Equal("rdf/newname.rdf", updated.ObjectKey);
        Assert.Equal("etag2", updated.Etag);
        Assert.Equal(200, updated.Size);
        Assert.Equal(newTime, updated.UpdatedAt);
        Assert.True(updated.CreatedAt < updated.UpdatedAt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
