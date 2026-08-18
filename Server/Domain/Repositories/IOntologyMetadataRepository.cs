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

namespace OntologyService.Domain.Repositories;

/// <summary>
/// Represents a repository for managing ontology metadata persisted in Postgres.
/// </summary>
/// <remarks>
/// Changes are staged on the underlying unit of work and only persisted when
/// <see cref="SaveChangesAsync"/> is called, allowing metadata changes to be committed
/// atomically together with outbox events.
/// </remarks>
public interface IOntologyMetadataRepository
{
    /// <summary>
    /// Stages a new ontology metadata entry for insertion.
    /// </summary>
    /// <param name="ontology">The ontology metadata to add.</param>
    void Add(Ontology ontology);

    /// <summary>
    /// Retrieves all ontology metadata entries, ordered by creation date (newest first).
    /// </summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A list of all stored ontology metadata entries.</returns>
    Task<List<Ontology>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single ontology metadata entry by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the ontology.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The ontology metadata, or <c>null</c> if it does not exist.</returns>
    Task<Ontology?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Stages an ontology metadata entry for deletion.
    /// </summary>
    /// <param name="ontology">The ontology metadata to remove.</param>
    void Remove(Ontology ontology);

    /// <summary>
    /// Persists all staged changes to the database.
    /// </summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
