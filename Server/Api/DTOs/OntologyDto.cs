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

namespace OntologyService.Api.DTOs;

/// <summary>
/// Represents the metadata of an uploaded ontology returned to API clients.
/// </summary>
public class OntologyDto
{
    /// <summary>The unique identifier of the ontology.</summary>
    public Guid Id { get; set; }

    /// <summary>The display name of the ontology (derived from the file name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The original uploaded file name including its extension.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The file format (e.g. <c>ttl</c> or <c>rdf</c>).</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>The content type (MIME type) of the stored file.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The size of the stored file in bytes.</summary>
    public long Size { get; set; }

    /// <summary>The ETag of the stored file.</summary>
    public string Etag { get; set; } = string.Empty;

    /// <summary>The timestamp when the ontology was first uploaded.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>The timestamp when the ontology file was last replaced.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Creates an <see cref="OntologyDto"/> from an <see cref="Ontology"/> domain entity.
    /// </summary>
    /// <param name="ontology">The source ontology metadata.</param>
    /// <returns>A populated <see cref="OntologyDto"/>.</returns>
    public static OntologyDto FromEntity(Ontology ontology) => new()
    {
        Id = ontology.Id,
        Name = ontology.Name,
        FileName = ontology.FileName,
        Format = ontology.Format,
        ContentType = ontology.ContentType,
        Size = ontology.Size,
        Etag = ontology.Etag,
        CreatedAt = ontology.CreatedAt,
        UpdatedAt = ontology.UpdatedAt
    };
}
