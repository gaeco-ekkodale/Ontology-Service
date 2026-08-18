// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace OntologyService.Api.Options;

/// <summary>
/// Represents the configuration options for the MinIO service.
/// </summary>
public class MinioOptions
{
    /// <summary>
    /// The name of the configuration section for MinIO settings.
    /// </summary>
    public const string SectionName = "Minio";

    /// <summary>
    /// Gets or sets the address of the MinIO server.
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// Gets or sets the access key for authenticating with the MinIO server.
    /// </summary>
    public required string AccessKey { get; set; }

    /// <summary>
    /// Gets or sets the secret key for authenticating with the MinIO server.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the name of the bucket used for storing ontologies.
    /// </summary>
    public required string OntologyBucket { get; set; }
}
