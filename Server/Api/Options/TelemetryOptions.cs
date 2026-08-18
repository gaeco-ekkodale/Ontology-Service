// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace Ekkodale.TelemetryExtensions;

/// <summary>
/// Configuration of the telemetry export, bound from the "OpenTelemetry" configuration section.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Name of the configuration section holding these options.
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// Endpoint of the OTLP collector traces, metrics and logs are exported to.
    /// </summary>
    public string OtlpEndpoint { get; set; } = string.Empty;
}
