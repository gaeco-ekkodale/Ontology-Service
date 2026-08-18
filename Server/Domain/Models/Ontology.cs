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
using System.ComponentModel.DataAnnotations.Schema;

namespace OntologyService.Domain.Models;

/// <summary>
/// Represents the metadata of an uploaded ontology. The ontology file itself is stored in
/// object storage (MinIO); this entity holds the relational metadata persisted in Postgres.
/// </summary>
[Table("ontology")]
public class Ontology
{
    /// <summary>
    /// Gets the unique identifier of the ontology (server-generated GUID).
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the display name of the ontology, derived from the uploaded file name (without extension).
    /// </summary>
    [Required]
    [MaxLength(260)]
    [Column("name")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the original file name of the uploaded ontology, including its extension.
    /// </summary>
    [Required]
    [MaxLength(260)]
    [Column("file_name")]
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the file format of the ontology (e.g. <c>ttl</c> or <c>rdf</c>).
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("format")]
    public string Format { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the content type (MIME type) of the stored file.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("content_type")]
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the object storage bucket where the file is stored.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("bucket")]
    public string Bucket { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the object key (path) of the file within the storage bucket.
    /// </summary>
    [Required]
    [MaxLength(300)]
    [Column("object_key")]
    public string ObjectKey { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the ETag of the stored file as reported by the object storage.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("etag")]
    public string Etag { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the size of the stored file in bytes.
    /// </summary>
    [Required]
    [Column("size")]
    public long Size { get; private set; }

    /// <summary>
    /// Gets the timestamp when the ontology was first uploaded.
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the ontology file was last replaced.
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ontology"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the ontology.</param>
    /// <param name="name">The display name derived from the file name.</param>
    /// <param name="fileName">The original uploaded file name.</param>
    /// <param name="format">The file format (e.g. <c>ttl</c> or <c>rdf</c>).</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="bucket">The storage bucket name.</param>
    /// <param name="objectKey">The object key within the bucket.</param>
    /// <param name="etag">The ETag of the stored file.</param>
    /// <param name="size">The size of the file in bytes.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    public Ontology(Guid id, string name, string fileName, string format, string contentType,
        string bucket, string objectKey, string etag, long size, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        FileName = fileName;
        Format = format;
        ContentType = contentType;
        Bucket = bucket;
        ObjectKey = objectKey;
        Etag = etag;
        Size = size;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Replaces the stored file metadata after the underlying ontology file has been overwritten.
    /// The <see cref="Id"/> and <see cref="CreatedAt"/> remain unchanged.
    /// </summary>
    /// <param name="name">The display name derived from the new file name.</param>
    /// <param name="fileName">The new uploaded file name.</param>
    /// <param name="format">The new file format.</param>
    /// <param name="contentType">The new content type.</param>
    /// <param name="objectKey">The new object key within the bucket.</param>
    /// <param name="etag">The new ETag of the stored file.</param>
    /// <param name="size">The new file size in bytes.</param>
    /// <param name="updatedAt">The update timestamp.</param>
    public void ReplaceFile(string name, string fileName, string format, string contentType,
        string objectKey, string etag, long size, DateTimeOffset updatedAt)
    {
        Name = name;
        FileName = fileName;
        Format = format;
        ContentType = contentType;
        ObjectKey = objectKey;
        Etag = etag;
        Size = size;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ontology"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is required by Entity Framework Core for materialization.
    /// </remarks>
    private Ontology() { }
}
