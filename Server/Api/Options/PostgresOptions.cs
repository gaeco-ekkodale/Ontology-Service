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

/// <summary>
/// Represents the configuration options for a PostgreSQL database connection.
/// </summary>
public class PostgresOptions
{
    /// <summary>
    /// JSON configuration section name for PostgreSQL database connection settings.
    /// </summary>
    public const string Postgres = "Postgres";

    /// <summary>
    /// Gets or sets the database server host.
    /// Defaults to 'localhost'.
    /// </summary>
    [RegularExpression(@"^(localhost|[a-zA-Z0-9.-]+)$", ErrorMessage = "Value for {0} must be a valid host.")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database server port.
    /// Defaults to 5432.
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Gets or sets the name of the database.
    /// Defaults to 'postgres'.
    /// </summary>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string Database { get; set; } = "postgres";

    /// <summary>
    /// Gets or sets the username for the database connection.
    /// </summary>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for the database connection.
    /// </summary>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string Password { get; set; } = string.Empty;
}
