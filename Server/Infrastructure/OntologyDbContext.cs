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

namespace OntologyService.Infrastructure;

/// <summary>
/// Represents the database context for the ontology service, providing access to the underlying database
/// and managing the entity models.
/// </summary>
public class OntologyDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="OutboxEvent"/> entities.
    /// </summary>
    public DbSet<OutboxEvent> OutboxEvents { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Ontology"/> metadata entities.
    /// </summary>
    public DbSet<Ontology> Ontologies { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OntologyDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public OntologyDbContext(DbContextOptions<OntologyDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types.
    /// </summary>
    /// <remarks>
    /// This method is used to define the entity relationships, constraints, and other model configurations
    /// using the Fluent API. Specifically, it configures the primary key for the <see cref="OutboxEvent"/>
    /// and sets the column type for its Payload property.
    /// </remarks>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<OutboxEvent>()
            .Property(p => p.Payload)
            .HasColumnType("text")
            .IsRequired(false);

        modelBuilder.Entity<Ontology>().HasKey(x => x.Id);

        base.OnModelCreating(modelBuilder);
    }
}
