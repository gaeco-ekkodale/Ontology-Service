// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OntologyService.Api.DTOs;
using OntologyService.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace OntologyService.Api.Controllers;

/// <summary>
/// API controller for managing multiple ontologies. Each ontology file is stored in object storage
/// while its metadata is persisted relationally; every change emits a Kafka event via the outbox.
/// </summary>
[Route("[controller]")]
[ApiController]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Authorize]
public class OntologyController : ControllerBase
{
    private readonly OntologyAppService _ontologyService;
    private readonly ILogger<OntologyController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OntologyController"/> class.
    /// </summary>
    /// <param name="ontologyService">The application service orchestrating ontology operations.</param>
    /// <param name="logger">The logger.</param>
    public OntologyController(OntologyAppService ontologyService, ILogger<OntologyController> logger)
    {
        _ontologyService = ontologyService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a new ontology (Turtle or RDF). A new identifier is generated server-side.
    /// </summary>
    /// <param name="file">The ontology turtle- or rdf-file to upload.</param>
    /// <returns>The metadata of the created ontology.</returns>
    /// <response code="201">The ontology was created.</response>
    /// <response code="400">The file is missing or invalid.</response>
    [SwaggerOperation(
        Summary = "Uploads a new ontology",
        Description = "Uploads an ontology turtle- or rdf-file, stores it, and creates its metadata. A new id is generated.",
        OperationId = "UploadOntology",
        Tags = new[] { "ontology" }
    )]
    [ProducesResponseType(typeof(OntologyDto), StatusCodes.Status201Created)]
    [HttpPost]
    public async Task<IActionResult> UploadOntology(IFormFile file, CancellationToken ct)
    {
        var result = await _ontologyService.CreateAsync(file, ct);
        if (!result.Success)
            return BadRequest(result.Error);

        var dto = OntologyDto.FromEntity(result.Ontology!);
        return CreatedAtAction(nameof(GetOntology), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Returns the metadata of all uploaded ontologies.
    /// </summary>
    /// <returns>A list of ontology metadata entries.</returns>
    /// <response code="200">Success.</response>
    [SwaggerOperation(
        Summary = "Lists all ontologies",
        Description = "Returns metadata for all uploaded ontologies.",
        OperationId = "GetOntologies",
        Tags = new[] { "ontology" }
    )]
    [ProducesResponseType(typeof(IEnumerable<OntologyDto>), StatusCodes.Status200OK)]
    [HttpGet("/ontologies")]
    public async Task<IActionResult> GetOntologies(CancellationToken ct)
    {
        var ontologies = await _ontologyService.GetAllAsync(ct);
        return Ok(ontologies.Select(OntologyDto.FromEntity));
    }

    /// <summary>
    /// Returns the metadata of a single ontology.
    /// </summary>
    /// <param name="id">The identifier of the ontology.</param>
    /// <returns>The ontology metadata.</returns>
    /// <response code="200">Success.</response>
    /// <response code="404">No ontology exists with the given id.</response>
    [SwaggerOperation(
        Summary = "Gets a single ontology",
        Description = "Returns the metadata of the ontology with the given id.",
        OperationId = "GetOntology",
        Tags = new[] { "ontology" }
    )]
    [ProducesResponseType(typeof(OntologyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOntology(Guid id, CancellationToken ct)
    {
        var ontology = await _ontologyService.GetByIdAsync(id, ct);
        if (ontology is null)
            return NotFound();

        return Ok(OntologyDto.FromEntity(ontology));
    }

    /// <summary>
    /// Downloads the raw ontology file.
    /// </summary>
    /// <param name="id">The identifier of the ontology.</param>
    /// <returns>The ontology file content.</returns>
    /// <response code="200">Success.</response>
    /// <response code="404">No ontology exists with the given id.</response>
    [SwaggerOperation(
        Summary = "Downloads the ontology file",
        Description = "Returns the raw stored ontology file (turtle or rdf).",
        OperationId = "GetOntologyFile",
        Tags = new[] { "ontology" }
    )]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> GetOntologyFile(Guid id, CancellationToken ct)
    {
        var fileResult = await _ontologyService.GetFileAsync(id, ct);
        if (fileResult is null)
            return NotFound();

        return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
    }

    /// <summary>
    /// Replaces the file of an existing ontology. The ontology id is preserved.
    /// </summary>
    /// <param name="id">The identifier of the ontology to update.</param>
    /// <param name="file">The new ontology file.</param>
    /// <returns>The updated ontology metadata.</returns>
    /// <response code="200">The ontology file was replaced.</response>
    /// <response code="400">The file is missing or invalid.</response>
    /// <response code="404">No ontology exists with the given id.</response>
    [SwaggerOperation(
        Summary = "Replaces an ontology file",
        Description = "Overwrites the file of an existing ontology and notifies downstream services.",
        OperationId = "UpdateOntology",
        Tags = new[] { "ontology" }
    )]
    [ProducesResponseType(typeof(OntologyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateOntology(Guid id, IFormFile file, CancellationToken ct)
    {
        var result = await _ontologyService.ReplaceFileAsync(id, file, ct);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(result.Error);

        return Ok(OntologyDto.FromEntity(result.Ontology!));
    }

    /// <summary>
    /// Deletes an ontology, its file, and notifies downstream services.
    /// </summary>
    /// <param name="id">The identifier of the ontology to delete.</param>
    /// <response code="204">The ontology was deleted.</response>
    /// <response code="404">No ontology exists with the given id.</response>
    [SwaggerOperation(
        Summary = "Deletes an ontology",
        Description = "Deletes the ontology metadata and file and notifies downstream services.",
        OperationId = "DeleteOntology",
        Tags = new[] { "ontology" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOntology(Guid id, CancellationToken ct)
    {
        var deleted = await _ontologyService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
