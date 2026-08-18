// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;

namespace OntologyService.Events;

/// <summary>
/// Contains details about an ontology file that has been uploaded to storage.
/// </summary>
public class UploadedOntologyFile
{
    /// <summary>
    /// Gets or sets the unique identifier of the ontology.
    /// </summary>
    [Required]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the ontology (derived from the file name).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the ETag (entity tag) of the uploaded file.
    /// </summary>
    [Required]
    public required string Etag { get; set; }

    /// <summary>
    /// Gets or sets the name of the storage bucket where the file is located.
    /// </summary>
    [Required]
    public required string Bucket { get; set; }

    /// <summary>
    /// Gets or sets the object key (file path) within the storage bucket.
    /// </summary>
    [Required]
    public required string ObjectKey { get; set; }

    /// <summary>
    /// Gets or sets the content type (MIME type) of the uploaded file.
    /// </summary>
    [Required]
    public required string ContentType { get; set; }
}