// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using OntologyService.Api.Validation;
using OntologyService.Domain.Models;
using OntologyService.Domain.Repositories;
using OntologyService.Events;

namespace OntologyService.Api.Services;

/// <summary>
/// The result of an ontology create or update operation.
/// </summary>
public class OntologyOperationResult
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets a value indicating whether the target ontology was not found.</summary>
    public bool NotFound { get; init; }

    /// <summary>Gets the error message when the operation failed validation.</summary>
    public string? Error { get; init; }

    /// <summary>Gets the resulting ontology metadata on success.</summary>
    public Ontology? Ontology { get; init; }

    /// <summary>Creates a successful result wrapping the given ontology.</summary>
    public static OntologyOperationResult Ok(Ontology ontology) => new() { Success = true, Ontology = ontology };

    /// <summary>Creates a validation-failure result with the given message.</summary>
    public static OntologyOperationResult Invalid(string error) => new() { Success = false, Error = error };

    /// <summary>Creates a not-found result.</summary>
    public static OntologyOperationResult MissingOntology() => new() { Success = false, NotFound = true };
}

/// <summary>
/// The downloadable content of an ontology file together with its metadata.
/// </summary>
/// <param name="Content">The file content stream.</param>
/// <param name="ContentType">The content type of the file.</param>
/// <param name="FileName">The original file name.</param>
public record OntologyFileResult(Stream Content, string ContentType, string FileName);

/// <summary>
/// Orchestrates ontology lifecycle operations: validating the uploaded file, storing it in object
/// storage, persisting its metadata, and emitting an outbox event so downstream services are notified.
/// </summary>
public class OntologyAppService
{
    private readonly IOntologyStorageRepository _storage;
    private readonly IOntologyMetadataRepository _metadata;
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<OntologyAppService> _logger;
    private readonly string _bucket;
    private readonly string _topic;

    /// <summary>
    /// Initializes a new instance of the <see cref="OntologyAppService"/> class.
    /// </summary>
    /// <param name="storage">The object storage repository for ontology files.</param>
    /// <param name="metadata">The metadata repository for ontology entries.</param>
    /// <param name="outbox">The outbox repository used to emit change events.</param>
    /// <param name="configuration">The application configuration (bucket and topic names).</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if the ontology Kafka topic is not configured.</exception>
    public OntologyAppService(
        IOntologyStorageRepository storage,
        IOntologyMetadataRepository metadata,
        IOutboxRepository outbox,
        IConfiguration configuration,
        ILogger<OntologyAppService> logger)
    {
        _storage = storage;
        _metadata = metadata;
        _outbox = outbox;
        _logger = logger;
        _bucket = configuration["Minio:OntologyBucket"] ?? "ontology";
        _topic = configuration["Kafka:Topics:Ontology"]
            ?? throw new ArgumentNullException("Kafka:Topics:Ontology configuration is missing");
    }

    /// <summary>
    /// Returns metadata for all stored ontologies.
    /// </summary>
    public Task<List<Ontology>> GetAllAsync(CancellationToken ct = default) => _metadata.GetAllAsync(ct);

    /// <summary>
    /// Returns metadata for a single ontology, or <c>null</c> if it does not exist.
    /// </summary>
    public Task<Ontology?> GetByIdAsync(Guid id, CancellationToken ct = default) => _metadata.GetByIdAsync(id, ct);

    /// <summary>
    /// Downloads the raw ontology file together with its content type and file name.
    /// </summary>
    /// <returns>The file result, or <c>null</c> if the ontology does not exist.</returns>
    public async Task<OntologyFileResult?> GetFileAsync(Guid id, CancellationToken ct = default)
    {
        var ontology = await _metadata.GetByIdAsync(id, ct);
        if (ontology is null) return null;

        var content = await _storage.GetAsync(ontology.Bucket, ontology.ObjectKey);
        return new OntologyFileResult(content, ontology.ContentType, ontology.FileName);
    }

    /// <summary>
    /// Validates and stores a new ontology: uploads the file to object storage, persists its metadata,
    /// and emits an <see cref="UploadedOntologyFile"/> event. A new GUID is generated server-side.
    /// </summary>
    /// <param name="file">The uploaded ontology file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The operation result.</returns>
    public async Task<OntologyOperationResult> CreateAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return OntologyOperationResult.Invalid("File is null or empty.");

        if (!OntologyValidator.TryGetFormat(file.FileName, out var format, out var contentType))
            return OntologyOperationResult.Invalid("File is not a turtle or rdf file.");

        await using (var validationStream = file.OpenReadStream())
        {
            if (!OntologyValidator.IsValid(validationStream, format))
                return OntologyOperationResult.Invalid("File is not a valid turtle or rdf file.");
        }

        var id = Guid.NewGuid();
        var objectKey = BuildObjectKey(format, id);
        var name = Path.GetFileNameWithoutExtension(file.FileName);

        await using var uploadStream = file.OpenReadStream();
        var response = await _storage.UploadAsync(_bucket, objectKey, uploadStream, contentType, file.Length);

        var ontology = new Ontology(id, name, file.FileName, format, contentType,
            _bucket, objectKey, response.Etag, file.Length, DateTimeOffset.UtcNow);

        _metadata.Add(ontology);
        _outbox.Add(new UploadedOntologyFile
        {
            Id = id.ToString(),
            Name = name,
            Etag = response.Etag,
            Bucket = _bucket,
            ObjectKey = objectKey,
            ContentType = contentType
        }, _topic, id.ToString());

        // File is already in storage; only metadata+event commit remains.
        if (!await TryCommitAsync(ct))
        {
            // Commit failed; attempt to clean up the orphaned storage object.
            // If this cleanup also fails, the object is orphaned with no metadata.
            await SafeRemoveAsync(_bucket, objectKey);
            return OntologyOperationResult.Invalid("Failed to persist ontology metadata.");
        }

        return OntologyOperationResult.Ok(ontology);
    }

    /// <summary>
    /// Replaces the file of an existing ontology. The metadata identity (<see cref="Ontology.Id"/> and
    /// creation timestamp) is preserved while file-related fields are updated and an
    /// <see cref="UploadedOntologyFile"/> event is emitted so downstream services re-process the file.
    /// </summary>
    /// <param name="id">The identifier of the ontology to update.</param>
    /// <param name="file">The new ontology file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The operation result.</returns>
    public async Task<OntologyOperationResult> ReplaceFileAsync(Guid id, IFormFile file, CancellationToken ct = default)
    {
        var ontology = await _metadata.GetByIdAsync(id, ct);
        if (ontology is null)
            return OntologyOperationResult.MissingOntology();

        if (file == null || file.Length == 0)
            return OntologyOperationResult.Invalid("File is null or empty.");

        if (!OntologyValidator.TryGetFormat(file.FileName, out var format, out var contentType))
            return OntologyOperationResult.Invalid("File is not a turtle or rdf file.");

        await using (var validationStream = file.OpenReadStream())
        {
            if (!OntologyValidator.IsValid(validationStream, format))
                return OntologyOperationResult.Invalid("File is not a valid turtle or rdf file.");
        }

        var oldObjectKey = ontology.ObjectKey;
        var newObjectKey = BuildObjectKey(format, id);
        var name = Path.GetFileNameWithoutExtension(file.FileName);

        await using var uploadStream = file.OpenReadStream();
        var response = await _storage.UploadAsync(_bucket, newObjectKey, uploadStream, contentType, file.Length);

        ontology.ReplaceFile(name, file.FileName, format, contentType,
            newObjectKey, response.Etag, file.Length, DateTimeOffset.UtcNow);

        _outbox.Add(new UploadedOntologyFile
        {
            Id = id.ToString(),
            Name = name,
            Etag = response.Etag,
            Bucket = _bucket,
            ObjectKey = newObjectKey,
            ContentType = contentType
        }, _topic, id.ToString());

        // New file is already in storage; only metadata+event commit remains.
        // SaveChangesAsync should return >= 1 since ReplaceFile always modifies UpdatedAt.
        // However, in edge cases where EF detects no change, it may return 0 without throwing.
        // For Replace, we only treat an exception as failure; assume success if no exception.
        try
        {
            await _metadata.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist ontology metadata for replacement");
            // Commit failed; attempt to clean up the new storage object only if it does not overwrite the old one.
            // If the format changed, they have different keys; the old one is still needed for rollback.
            // If this cleanup also fails, the new object is orphaned with no metadata.
            if (!string.Equals(oldObjectKey, newObjectKey, StringComparison.Ordinal))
                await SafeRemoveAsync(_bucket, newObjectKey);
            return OntologyOperationResult.Invalid("Failed to persist ontology metadata.");
        }

        // Commit succeeded. The format (and therefore the object key) may have changed; if so,
        // remove the now-orphaned old file. This is cleanup after success, not critical for correctness.
        if (!string.Equals(oldObjectKey, newObjectKey, StringComparison.Ordinal))
            await SafeRemoveAsync(ontology.Bucket, oldObjectKey);

        return OntologyOperationResult.Ok(ontology);
    }

    /// <summary>
    /// Deletes an ontology: removes its metadata, emits a <see cref="DeletedOntology"/> event, and
    /// removes the underlying file from object storage.
    /// </summary>
    /// <param name="id">The identifier of the ontology to delete.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns><c>true</c> if the ontology existed and was deleted; otherwise <c>false</c>.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var ontology = await _metadata.GetByIdAsync(id, ct);
        if (ontology is null)
            return false;

        _metadata.Remove(ontology);
        _outbox.Add(new DeletedOntology
        {
            Id = id.ToString(),
            Bucket = ontology.Bucket,
            ObjectKey = ontology.ObjectKey
        }, _topic, id.ToString());

        await _metadata.SaveChangesAsync(ct);

        // Metadata and the delete event are committed; remove the file as a best-effort cleanup.
        await SafeRemoveAsync(ontology.Bucket, ontology.ObjectKey);

        return true;
    }

    private static string BuildObjectKey(string format, Guid id) => $"{format}/{id}.{format}";

    private async Task<bool> TryCommitAsync(CancellationToken ct)
    {
        var saved = await _metadata.SaveChangesAsync(ct);
        return saved > 0;
    }

    private async Task SafeRemoveAsync(string bucket, string objectKey)
    {
        try
        {
            await _storage.RemoveAsync(bucket, objectKey);
        }
        catch (Exception ex)
        {
            // WARNING: This is a best-effort cleanup that silently fails. If called as compensating action
            // after a metadata commit failure (Create/Replace), a failure here leaves an orphaned object in
            // storage with no metadata and no cleanup event. Orphans must be discovered and cleaned manually.
            _logger.LogError(ex, "Failed to remove orphaned ontology object {ObjectKey} from bucket {Bucket}. " +
                "This object may have metadata and cleanup event, or may be abandoned with no tracking. " +
                "Check metadata DB and outbox events.",
                objectKey, bucket);
        }
    }
}
