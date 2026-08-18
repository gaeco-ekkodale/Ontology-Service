// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using VDS.RDF;
using VDS.RDF.Parsing;

namespace OntologyService.Api.Validation;

/// <summary>
/// Provides validation and format detection for uploaded ontology files (Turtle / RDF-XML).
/// </summary>
public static class OntologyValidator
{
    /// <summary>The Turtle file format identifier.</summary>
    public const string FormatTurtle = "ttl";

    /// <summary>The RDF-XML file format identifier.</summary>
    public const string FormatRdf = "rdf";

    /// <summary>The content type used for Turtle files.</summary>
    public const string ContentTypeTurtle = "text/turtle";

    /// <summary>The content type used for RDF-XML files.</summary>
    public const string ContentTypeRdf = "application/rdf+xml";

    /// <summary>
    /// Determines the ontology format from a file name based on its extension.
    /// </summary>
    /// <param name="fileName">The uploaded file name.</param>
    /// <param name="format">The detected format (<see cref="FormatTurtle"/> or <see cref="FormatRdf"/>).</param>
    /// <param name="contentType">The matching content type.</param>
    /// <returns><c>true</c> if the file is a supported Turtle or RDF file; otherwise <c>false</c>.</returns>
    public static bool TryGetFormat(string fileName, out string format, out string contentType)
    {
        if (fileName.EndsWith(".ttl", StringComparison.OrdinalIgnoreCase))
        {
            format = FormatTurtle;
            contentType = ContentTypeTurtle;
            return true;
        }
        if (fileName.EndsWith(".rdf", StringComparison.OrdinalIgnoreCase))
        {
            format = FormatRdf;
            contentType = ContentTypeRdf;
            return true;
        }

        format = string.Empty;
        contentType = string.Empty;
        return false;
    }

    /// <summary>
    /// Validates that the provided stream contains a parseable ontology of the given format.
    /// The stream position is left unchanged for the caller is responsible for resetting it.
    /// </summary>
    /// <param name="stream">The file stream to validate.</param>
    /// <param name="format">The expected format (<see cref="FormatTurtle"/> or <see cref="FormatRdf"/>).</param>
    /// <returns><c>true</c> if the content is valid; otherwise <c>false</c>.</returns>
    public static bool IsValid(Stream stream, string format)
    {
        try
        {
            using StreamReader reader = new(stream, leaveOpen: true);
            Graph graph = new();

            if (format == FormatTurtle)
            {
                new TurtleParser().Load(graph, reader);
                return true;
            }
            if (format == FormatRdf)
            {
                new RdfXmlParser().Load(graph, reader);
                return graph.Triples.Count > 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
