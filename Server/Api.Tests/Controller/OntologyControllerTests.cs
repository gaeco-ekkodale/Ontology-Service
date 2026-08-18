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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OntologyService.Api.DTOs;
using OntologyService.Api.Services;
using OntologyService.Domain.Models;
using OntologyService.Domain.Repositories;
using Xunit;

namespace OntologyService.Api.Controllers.Tests;

public class OntologyControllerTests
{
	private readonly IOntologyStorageRepository _storage;
	private readonly IOntologyMetadataRepository _metadata;
	private readonly IOutboxRepository _outbox;
	private readonly IConfiguration _config;
	private readonly ILogger<OntologyAppService> _serviceLogger;
	private readonly OntologyAppService _appService;
	private readonly ILogger<OntologyController> _logger;
	private readonly OntologyController _controller;

	public OntologyControllerTests()
	{
		_storage = Substitute.For<IOntologyStorageRepository>();
		_metadata = Substitute.For<IOntologyMetadataRepository>();
		_outbox = Substitute.For<IOutboxRepository>();
		_serviceLogger = Substitute.For<ILogger<OntologyAppService>>();

		_config = Substitute.For<IConfiguration>();
		_config["Minio:OntologyBucket"].Returns("ontology");
		_config["Kafka:Topics:Ontology"].Returns("ontology-topic");

		_appService = new OntologyAppService(_storage, _metadata, _outbox, _config, _serviceLogger);
		_logger = Substitute.For<ILogger<OntologyController>>();
		_controller = new OntologyController(_appService, _logger);
	}

	[Fact]
	public async Task UploadOntology_WithValidFile_Returns201Created()
	{
		var fileContent = """
			@prefix ex: <http://example.org/> .
			ex:s ex:p ex:o .
			""";
		var mockFile = CreateMockFormFile("ontology.ttl", fileContent);

		_storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long?>())
			.Returns(new Minio.DataModel.Response.PutObjectResponse(System.Net.HttpStatusCode.OK, "ontology", new Dictionary<string, string>(), 0, "ttl/id.ttl") { Etag = "etag1" });

		_metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns(1);

		var result = await _controller.UploadOntology(mockFile, CancellationToken.None);

		var createdResult = Assert.IsType<CreatedAtActionResult>(result);
		Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
		Assert.Equal(nameof(OntologyController.GetOntology), createdResult.ActionName);
		Assert.True(createdResult.RouteValues!.ContainsKey("id"));
		Assert.IsType<OntologyDto>(createdResult.Value);
	}

	[Fact]
	public async Task UploadOntology_WithInvalidFile_Returns400BadRequest()
	{
		var mockFile = CreateMockFormFile("ontology.txt", "invalid content");

		var result = await _controller.UploadOntology(mockFile, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Contains("not a turtle or rdf file", badRequest.Value!.ToString()!);
	}

	[Fact]
	public async Task GetOntologies_ReturnsAllOntologies()
	{
		var ontologies = new List<Ontology>
		{
			new(Guid.NewGuid(), "name1", "name1.ttl", "ttl", "text/turtle", "ontology", "ttl/id1.ttl", "etag1", 100, DateTimeOffset.UtcNow),
			new(Guid.NewGuid(), "name2", "name2.rdf", "rdf", "application/rdf+xml", "ontology", "rdf/id2.rdf", "etag2", 200, DateTimeOffset.UtcNow)
		};

		_metadata.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(ontologies);

		var result = await _controller.GetOntologies(CancellationToken.None);

		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

		var dtos = Assert.IsAssignableFrom<IEnumerable<OntologyDto>>(okResult.Value);
		Assert.Equal(2, dtos.Count());
	}

	[Fact]
	public async Task GetOntology_WithExistingId_ReturnsOntology()
	{
		var id = Guid.NewGuid();
		var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
			"ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns(ontology);

		var result = await _controller.GetOntology(id, CancellationToken.None);

		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

		var dto = Assert.IsType<OntologyDto>(okResult.Value);
		Assert.Equal(id, dto.Id);
		Assert.Equal("name", dto.Name);
	}

	[Fact]
	public async Task GetOntology_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns((Ontology?)null);

		var result = await _controller.GetOntology(id, CancellationToken.None);

		var notFoundResult = Assert.IsType<NotFoundResult>(result);
		Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
	}

	[Fact]
	public async Task GetOntologyFile_WithExistingId_ReturnsFile()
	{
		var id = Guid.NewGuid();
		var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
			"ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

		var fileContent = "@prefix ex: <http://example.org/> . ex:s ex:p ex:o .";
		var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns(ontology);

		_storage.GetAsync(Arg.Any<string>(), Arg.Any<string>())
			.Returns(fileStream);

		var result = await _controller.GetOntologyFile(id, CancellationToken.None);

		var fileActionResult = Assert.IsType<FileStreamResult>(result);
		Assert.Equal("text/turtle", fileActionResult.ContentType);
		Assert.Equal("name.ttl", fileActionResult.FileDownloadName);
	}

	[Fact]
	public async Task GetOntologyFile_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns((Ontology?)null);

		var result = await _controller.GetOntologyFile(id, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task UpdateOntology_WithValidFile_Returns200Ok()
	{
		var id = Guid.NewGuid();
		var existingOntology = new Ontology(id, "old", "old.ttl", "ttl", "text/turtle",
			"ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

		var rdfContent = """
			<?xml version="1.0" encoding="UTF-8"?>
			<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns:ex="http://example.org/">
			  <rdf:Description rdf:about="http://example.org/s">
			    <ex:p rdf:resource="http://example.org/o"/>
			  </rdf:Description>
			</rdf:RDF>
			""";
		var mockFile = CreateMockFormFile("new.rdf", rdfContent);

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns(existingOntology);

		_storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long?>())
			.Returns(new Minio.DataModel.Response.PutObjectResponse(System.Net.HttpStatusCode.OK, "ontology", new Dictionary<string, string>(), 0, "rdf/id.rdf") { Etag = "etag2" });

		_metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns(1);

		var result = await _controller.UpdateOntology(id, mockFile, CancellationToken.None);

		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

		var dto = Assert.IsType<OntologyDto>(okResult.Value);
		Assert.Equal("rdf", dto.Format);
	}

	[Fact]
	public async Task UpdateOntology_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();
		var mockFile = CreateMockFormFile("new.ttl", "@prefix ex: <http://example.org/> . ex:s ex:p ex:o .");

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns((Ontology?)null);

		var result = await _controller.UpdateOntology(id, mockFile, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task UpdateOntology_WithInvalidFile_Returns400BadRequest()
	{
		var id = Guid.NewGuid();
		var existingOntology = new Ontology(id, "old", "old.ttl", "ttl", "text/turtle",
			"ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

		var mockFile = CreateMockFormFile("invalid.txt", "invalid content");

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns(existingOntology);

		var result = await _controller.UpdateOntology(id, mockFile, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Contains("not a turtle or rdf file", badRequest.Value!.ToString()!);
	}

	[Fact]
	public async Task DeleteOntology_WithExistingId_Returns204NoContent()
	{
		var id = Guid.NewGuid();
		var ontology = new Ontology(id, "name", "name.ttl", "ttl", "text/turtle",
			"ontology", "ttl/id.ttl", "etag1", 100, DateTimeOffset.UtcNow);

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns(ontology);

		_metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns(1);

		var result = await _controller.DeleteOntology(id, CancellationToken.None);

		var noContentResult = Assert.IsType<NoContentResult>(result);
		Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
	}

	[Fact]
	public async Task DeleteOntology_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();

		_metadata.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns((Ontology?)null);

		var result = await _controller.DeleteOntology(id, CancellationToken.None);

		var notFoundResult = Assert.IsType<NotFoundResult>(result);
		Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
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
