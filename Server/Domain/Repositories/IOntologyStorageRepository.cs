// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Minio.DataModel.Response;

namespace OntologyService.Domain.Repositories;

/// <summary>
/// Represents a repository for storing and retrieving ontology files in object storage (MinIO).
/// </summary>
public interface IOntologyStorageRepository
{
    /// <summary>
    /// Uploads (or overwrites) a file in the specified bucket under the given object key.
    /// The bucket is created if it does not yet exist.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="objectKey">The object key (path) within the bucket.</param>
    /// <param name="fileStream">The stream of the file to upload. Can be non-seekable (e.g., from IFormFile.OpenReadStream()).</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="objectSize">Optional file size in bytes. Pass this for non-seekable streams (e.g., IFormFile.Length) to avoid NotSupportedException when accessing stream.Length.</param>
    /// <returns>The response from the object storage after uploading the file.</returns>
    Task<PutObjectResponse> UploadAsync(string bucketName, string objectKey, Stream fileStream, string contentType, long? objectSize = null);

    /// <summary>
    /// Removes a file from the specified bucket. Missing objects are ignored.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="objectKey">The object key (path) within the bucket.</param>
    Task RemoveAsync(string bucketName, string objectKey);

    /// <summary>
    /// Downloads a file from the specified bucket into a seekable in-memory stream.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="objectKey">The object key (path) within the bucket.</param>
    /// <returns>A stream positioned at the beginning of the file content.</returns>
    Task<Stream> GetAsync(string bucketName, string objectKey);
}
