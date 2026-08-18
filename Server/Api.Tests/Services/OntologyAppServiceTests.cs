// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OntologyService.Domain.Models;
using OntologyService.Domain.Repositories;
using OntologyService.Events;
using System.Net;
using Xunit;

namespace OntologyService.Api.Services.Tests;

public class OntologyAppServiceTests
{
    private readonly IOntologyStorageRepository _storage;
    private readonly IOntologyMetadataRepository _metadata;
    private readonly IOutboxRepository _outbox;
    private readonly IConfiguration _config;
    private readonly ILogger<OntologyAppService> _logger;
    private readonly OntologyAppService _service;

    public OntologyAppServiceTests()
    {
        _storage = Substitute.For<IOntologyStorageRepository>();
        _metadata = Substitute.For<IOntologyMetadataRepository>();
        _outbox = Substitute.For<IOutboxRepository>();
        _logger = Substitute.For<ILogger<OntologyAppService>>();

        _config = Substitute.For<IConfiguration>();
        _config["Minio:OntologyBucket"].Returns("ontology");
        _config["Kafka:Topics:Ontology"].Returns("ontology-topic");

        _service = new OntologyAppService(_storage, _metadata, _outbox, _config, _logger);
    }

    [Fact]
    public async Task CreateAsync_WithValidTurtleFile_SuccessfullyUploadsAndPersistsMetadata()
    {
        var fileContent = """
            @prefix ex: <http://example.org/> .
            ex:s ex:p ex:o .
            """;
        var mockFile = CreateMockFormFile("ontology.ttl", fileContent);

        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long?>())
            .Returns(new Minio.DataModel.Response.PutObjectResponse(HttpStatusCode.OK, "ontology", new Dictionary<string, string>(), 0, "ttl/some-id.ttl") { Etag = "etag123" });

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _service.CreateAsync(mockFile);

        Assert.True(result.Success);
        Assert.NotNull(result.Ontology);
        Assert.Equal("ttl", result.Ontology!.Format);
        Assert.Equal("text/turtle", result.Ontology.ContentType);

        await _storage.Received(1).UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), "text/turtle", Arg.Any<long?>());
        _metadata.Received(1).Add(Arg.Any<Ontology>());
        _outbox.Received(1).Add(Arg.Any<UploadedOntologyFile>(), "ontology-topic", Arg.Any<string>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidFileFormat_ReturnsValidationError()
    {
        var mockFile = CreateMockFormFile("ontology.txt", "invalid content");

        var result = await _service.CreateAsync(mockFile);

        Assert.False(result.Success);
        Assert.Contains("not a turtle or rdf file", result.Error);
        await _storage.DidNotReceive().UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long?>());
    }

    [Fact]
    public async Task CreateAsync_WithNullFile_ReturnsValidationError()
    {
        var result = await _service.CreateAsync(null!);

        Assert.False(result.Success);
        Assert.Contains("null or empty", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyFile_ReturnsValidationError()
    {
        var mockFile = CreateMockFormFile("ontology.ttl", "");

        var result = await _service.CreateAsync(mockFile);

        Assert.False(result.Success);
        Assert.Contains("null or empty", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTurtleContent_ReturnsValidationError()
    {
        var mockFile = CreateMockFormFile("ontology.ttl", "this is not valid turtle");

        var result = await _service.CreateAsync(mockFile);

        Assert.False(result.Success);
        Assert.Contains("not a valid turtle or rdf file", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenPersistenceFails_RollsBackStorageUpload()
    {
        var fileContent = """
            @prefix ex: <http://example.org/> .
            ex:s ex:p ex:o .
            """;
        var mockFile = CreateMockFormFile("ontology.ttl", fileContent);

        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long?>())
            .Returns(new Minio.DataModel.Response.PutObjectResponse(HttpStatusCode.OK, "ontology", new Dictionary<string, string>(), 0, "ttl/some-id.ttl") { Etag = "etag123" });

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _service.CreateAsync(mockFile);

        Assert.False(result.Success);
        Assert.Contains("Failed to persist", result.Error);

        await _storage.Received(1).RemoveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ReplaceFileAsync_WithValidFile_UpdatesMetadataAndEmitsEvent()
    {
        var id = Guid.NewGuid();
        var existingOntology = new Ontology(id, "old", "old.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        var newFileContent = """
            @prefix ex: <http://example.org/> .
            ex:x ex:y ex:z .
            """;
        var mockFile = CreateMockFormFile("new.ttl", newFileContent);

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(existingOntology);

        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long?>())
            .Returns(new Minio.DataModel.Response.PutObjectResponse(HttpStatusCode.OK, "ontology", new Dictionary<string, string>(), 0, "ttl/id.ttl") { Etag = "etag2" });

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _service.ReplaceFileAsync(id, mockFile);

        Assert.True(result.Success);
        Assert.Equal("new.ttl", result.Ontology!.FileName);
        Assert.Equal("etag2", result.Ontology.Etag);

        _outbox.Received(1).Add(Arg.Any<UploadedOntologyFile>(), "ontology-topic", id.ToString());
    }

    [Fact]
    public async Task ReplaceFileAsync_WithNonExistentId_ReturnsMissingOntology()
    {
        var id = Guid.NewGuid();
        var mockFile = CreateMockFormFile("new.ttl", "@prefix ex: <http://example.org/> . ex:s ex:p ex:o .");

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Ontology?)null);

        var result = await _service.ReplaceFileAsync(id, mockFile);

        Assert.False(result.Success);
        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task ReplaceFileAsync_WhenFormatChanges_RemovesOldFile()
    {
        var id = Guid.NewGuid();
        var existingOntology = new Ontology(id, "old", "old.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        var newFileContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns:ex="http://example.org/">
              <rdf:Description rdf:about="http://example.org/s">
                <ex:p rdf:resource="http://example.org/o"/>
              </rdf:Description>
            </rdf:RDF>
            """;
        var mockFile = CreateMockFormFile("new.rdf", newFileContent);

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(existingOntology);

        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long?>())
            .Returns(new Minio.DataModel.Response.PutObjectResponse(HttpStatusCode.OK, "ontology", new Dictionary<string, string>(), 0, "rdf/id.rdf") { Etag = "etag2" });

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _service.ReplaceFileAsync(id, mockFile);

        Assert.True(result.Success);

        await _storage.Received(1).RemoveAsync("ontology", "ttl/id.ttl");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingOntology_RemovesMetadataAndEmitsEvent()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(ontology);

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _service.DeleteAsync(id);

        Assert.True(result);

        _metadata.Received(1).Remove(ontology);
        _outbox.Received(1).Add(Arg.Any<DeletedOntology>(), "ontology-topic", id.ToString());
        await _storage.Received(1).RemoveAsync("ontology", "ttl/id.ttl");
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        var id = Guid.NewGuid();

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Ontology?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result);

        _metadata.DidNotReceive().Remove(Arg.Any<Ontology>());
    }

    [Fact]
    public async Task DeleteAsync_WhenStorageRemovalFails_LogsErrorButSucceeds()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(ontology);

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        _storage
            .RemoveAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new Exception("Storage error")));

        var result = await _service.DeleteAsync(id);

        Assert.True(result);
        _logger.Received().Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<Exception>(), Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingOntology_ReturnsOntology()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(ontology);

        var result = await _service.GetByIdAsync(id);

        Assert.Equal(ontology, result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOntologies()
    {
        var ontologies = new List<Ontology>
        {
            new(Guid.NewGuid(), "name1", "name1.ttl", "ttl", "text/turtle", "ontology", "ttl/id1.ttl", "etag1", 100, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "name2", "name2.rdf", "rdf", "application/rdf+xml", "ontology", "rdf/id2.rdf", "etag2", 200, DateTimeOffset.UtcNow)
        };

        _metadata.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ontologies);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(ontologies, result);
    }

    [Fact]
    public async Task GetFileAsync_WithExistingOntology_ReturnsFileContent()
    {
        var id = Guid.NewGuid();
        var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
            "ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

        var fileContent = "file content here";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(ontology);

        _storage.GetAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(fileStream);

        var result = await _service.GetFileAsync(id);

        Assert.NotNull(result);
        Assert.Equal("name.ttl", result!.FileName);
        Assert.Equal("text/turtle", result.ContentType);
    }

    [Fact]
    public async Task GetFileAsync_WithNonExistentOntology_ReturnsNull()
    {
        var id = Guid.NewGuid();

        _metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Ontology?)null);

        var result = await _service.GetFileAsync(id);

        Assert.Null(result);
    }

    private static IFormFile CreateMockFormFile(string fileName, string content)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var mock = Substitute.For<IFormFile>();

        mock.FileName.Returns(fileName);
        mock.Length.Returns(stream.Length);
        mock.OpenReadStream().Returns(stream);
        mock.ContentType.Returns("application/octet-stream");

        return mock;
    }
}
