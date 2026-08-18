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
/// Contains details about an ontology that has been deleted, so that downstream services
/// can remove any derived data associated with it.
/// </summary>
public class DeletedOntology
{
    /// <summary>
    /// Gets or sets the unique identifier of the deleted ontology.
    /// </summary>
    [Required]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the storage bucket where the file was located.
    /// </summary>
    [Required]
    public required string Bucket { get; set; }

    /// <summary>
    /// Gets or sets the object key (file path) of the deleted file within the storage bucket.
    /// </summary>
    [Required]
    public required string ObjectKey { get; set; }
}
