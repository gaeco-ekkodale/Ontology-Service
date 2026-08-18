// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Ekkodale.TelemetryExtensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Minio;
using OntologyService.Api.Extensions;
using OntologyService.Api.Options;
using OntologyService.Api.Producer;
using OntologyService.Api.Services;
using OntologyService.Domain.Repositories;
using OntologyService.Infrastructure;
using OntologyService.Infrastructure.Repositories;
using System.Reflection;
using Throw;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

TelemetryOptions? telOpts = configuration.GetSection("OpenTelemetry").Get<TelemetryOptions>();
telOpts.ThrowIfNull("OpenTelemetry configuration is missing");
builder.AddMonitoring(telOpts, Assembly.GetExecutingAssembly());

builder.Services.AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.Keycloak))
    .ValidateDataAnnotations();

builder.Services.AddOptions<KafkaOptions>()
    .Bind(builder.Configuration.GetSection(KafkaOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddOptions<MinioOptions>()
    .Bind(builder.Configuration.GetSection("Minio"))
    .ValidateDataAnnotations();

builder.Services.AddOptions<PostgresOptions>()
    .Bind(builder.Configuration.GetSection(PostgresOptions.Postgres))
    .ValidateDataAnnotations();

builder.Services.AddHttpClient();
builder.Services.AddPostgres();
builder.Services.AddScoped<IOntologyStorageRepository, OntologyStorageRepository>();
builder.Services.AddScoped<IOntologyMetadataRepository, OntologyMetadataRepository>();
builder.Services.AddScoped<OntologyAppService>();

builder.Services.AddControllers();

var minioOpts = configuration.GetSection("Minio").Get<MinioOptions>();
builder.Services.AddSingleton(sp =>
{
    return new MinioClient()
        .WithEndpoint(minioOpts.Address)
        .WithCredentials(
            minioOpts.AccessKey,
            minioOpts.SecretKey
        )
        .Build();
});

builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Ontology API",
        Description = "An ASP.NET Core Web API for the ontology service",
    });
    options.EnableAnnotations();
});

#region Authentication

builder.Services.AddKeycloakAuthentication(options =>
{
    configuration.GetSection("Keycloak").Bind(options);
});

#endregion Authentication

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllOrigins",
            builder => builder
                .AllowAnyOrigin()  // Allowing any origin
                .AllowAnyMethod()  // Allowing any HTTP method
                .AllowAnyHeader()); // Allowing any header
    });
}
else
{
    var allowedCorsOrigin = builder.Configuration["AllowedCorsOrigins:ServerUrl"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins(allowedCorsOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
    });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<OntologyDbContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Database Creation ensured.");
    }
    catch (Exception e)
    {
        logger.LogError(e, "Database Creation failed!");
        Console.WriteLine(e.Message);
    }
}

if (builder.Environment.IsDevelopment())
{
    app.UseCors("AllowAllOrigins");
} 
else
{
    app.UseCors("AllowSpecificOrigin");
}

// Respect reverse proxy headers (Traefik) for scheme/host
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();
app.UseForwardedHeaders(fwdOptions);

app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
        var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
        var basePath = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? httpReq.PathBase.Value ?? string.Empty;

        swagger.Servers = [
            new OpenApiServer { Url = $"{scheme}://{host}{basePath}" }
        ];
    });
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "v1");
    options.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();