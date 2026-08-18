// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.Exceptions;
using OntologyService.Domain.Repositories;

namespace OntologyService.Infrastructure.Repositories;

/// <summary>
/// Implements <see cref="IOntologyStorageRepository"/> for storing ontology files in MinIO object storage.
/// </summary>
public class OntologyStorageRepository : IOntologyStorageRepository
{
    private readonly IMinioClient _minioClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OntologyStorageRepository"/> class.
    /// </summary>
    /// <param name="minioClient">The MinIO client for interacting with the object storage.</param>
    public OntologyStorageRepository(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    /// <inheritdoc />
    public async Task<PutObjectResponse> UploadAsync(string bucketName, string objectKey, Stream fileStream, string contentType, long? objectSize = null)
    {
        try
        {
            bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }

            var args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithStreamData(fileStream)
                .WithContentType(contentType);

            // Only set object size if explicitly provided (to avoid NotSupportedException on non-seekable streams)
            if (objectSize.HasValue)
                args.WithObjectSize(objectSize.Value);

            var response = await _minioClient.PutObjectAsync(args);

            if (string.IsNullOrEmpty(response.ObjectName))
            {
                throw new Exception("Fehler beim Hochladen der Datei: ObjectName is null or empty.");
            }

            return response;
        }
        catch (MinioException e)
        {
            throw new Exception($"Fehler beim Hochladen der Datei: {e.Message}", e);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string bucketName, string objectKey)
    {
        try
        {
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey));
        }
        catch (MinioException e)
        {
            throw new Exception($"Fehler beim Löschen der Datei: {e.Message}", e);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetAsync(string bucketName, string objectKey)
    {
        try
        {
            var memoryStream = new MemoryStream();
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream)));

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        catch (MinioException e)
        {
            throw new Exception($"Fehler beim Laden der Datei: {e.Message}", e);
        }
    }
}
