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

namespace OntologyService.Api.Options;

/// <summary>
/// Provides a class to manage connection information for keycloak.
/// </summary>
public class KeycloakOptions
{
    public const string Keycloak = "Keycloak";

    /// <summary>
    /// Keycloak server url.
    /// </summary>
    [Required]
    public string ServerUrl { get; set; }

    /// <summary>
    /// Realm used in the connection.
    /// </summary>
    [Required]
    public string Realm { get; set; }

    /// <summary>
    /// Url to the realm.
    /// </summary>
    [Required]
    public string RealmUrl => $"{ServerUrl}/realms/{Realm}";

    /// <summary>
    /// ClientId used in the connection.
    /// </summary>
    [Required]
    public string ClientId { get; set; }

    /// <summary>
    /// ClientSecret used in the connection.
    /// </summary>
    [Required]
    public string ClientSecret { get; set; }
}