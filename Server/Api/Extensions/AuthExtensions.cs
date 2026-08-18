// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OntologyService.Api.Options;

namespace OntologyService.Api.Extensions;

/// <summary>
/// Provides extension methods for IServiceCollection with topic: Authentication and Authorization
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Add authentication with keycloak to the service collection.
    /// </summary>
    /// <param name="services">The service collection to which the services are added.</param>
    /// <param name="keycloakSettings">The keycloak settings to use for the authentication.</param>
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, KeycloakOptions keycloakSettings)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.IncludeErrorDetails = true;
                    cfg.Authority = keycloakSettings.RealmUrl;
                    cfg.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidIssuer = keycloakSettings.RealmUrl,
                        ValidateLifetime = true,
                    };
                });

        return services;
    }

    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, Action<KeycloakOptions> configureOptions)
    {
        var keycloakOptions = new KeycloakOptions();
        configureOptions(keycloakOptions);
        return services.AddKeycloakAuthentication(keycloakOptions);
    }
}