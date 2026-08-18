// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Serilog;
using System.Reflection;

namespace Ekkodale.TelemetryExtensions;

/// <summary>
/// Provides extension methods for adding monitoring services to the application.
/// This includes the configuration of health checks, telemetry, and monitoring services.
/// This could include things like Application Insights, Prometheus, health checks for databases, external services, and more.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Adds health checks and monitoring services to the service collection.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="telemetryOpts">Telemetry options holding the OpenTelemetry collector endpoint. E.g. Azure Aspire, Elasticsearch etc.</param>
    /// <param name="assembly">The assembly whose name is used as the service name.</param>
    /// <returns>The updated web application builder.</returns>
    /// <exception cref="InvalidOperationException">The configured OTLP endpoint is not an absolute URI.</exception>
    public static WebApplicationBuilder AddMonitoring(this WebApplicationBuilder builder, TelemetryOptions telemetryOpts, Assembly assembly)
    {
        builder.Services.AddHealthChecks();

        var serviceName = assembly.GetName().Name ?? "UnknownService";

        if (!Uri.TryCreate(telemetryOpts.OtlpEndpoint, UriKind.Absolute, out var otlpCollectorEndpoint))
            throw new InvalidOperationException(
                $"{TelemetryOptions.SectionName}:{nameof(TelemetryOptions.OtlpEndpoint)} is not a valid absolute URI.");

        builder.ConfigureLogging(otlpCollectorEndpoint, serviceName);
        builder.ConfigureMetrics(otlpCollectorEndpoint, serviceName);

        return builder;
    }

    private static void ConfigureLogging(this WebApplicationBuilder builder, Uri otlpCollectorEndpoint, string serviceName)
    {
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName)); // Attributes log records with the service name, so they correlate with traces and metrics
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.AddOtlpExporter(opts =>
            {
                opts.Endpoint = otlpCollectorEndpoint;
            });
        });


        var fileLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("./logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Logging.AddSerilog(fileLogger);
    }

    private static void ConfigureMetrics(this WebApplicationBuilder builder, Uri otlpCollectorEndpoint, string serviceName)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName); // Explicitly set the service name
            })
            .WithMetrics(metricBuilder =>
            {
                metricBuilder
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = otlpCollectorEndpoint;
                    });
            })
            .WithTracing(tracerBuilder =>
            {
                tracerBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = otlpCollectorEndpoint;
                    });
            });
    }
}
