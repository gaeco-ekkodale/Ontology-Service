// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.EntityFrameworkCore;
using OntologyService.Domain.Models;
using OntologyService.Domain.Repositories;

namespace OntologyService.Infrastructure.Repositories;

/// <summary>
/// Implements <see cref="IOntologyMetadataRepository"/> using an Entity Framework Core context.
/// </summary>
public class OntologyMetadataRepository : IOntologyMetadataRepository
{
    private readonly OntologyDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="OntologyMetadataRepository"/> class.
    /// </summary>
    /// <param name="context">The database context to be used for data operations.</param>
    public OntologyMetadataRepository(OntologyDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void Add(Ontology ontology)
    {
        if (ontology == null) throw new ArgumentNullException(nameof(ontology));
        _context.Ontologies.Add(ontology);
    }

    /// <inheritdoc />
    public async Task<List<Ontology>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Ontologies
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Ontology?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Ontologies.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    /// <inheritdoc />
    public void Remove(Ontology ontology)
    {
        if (ontology == null) throw new ArgumentNullException(nameof(ontology));
        _context.Ontologies.Remove(ontology);
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
