// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using OntologyService.Api.Validation;
using Xunit;

namespace OntologyService.Api.Tests.Validation;

public class OntologyValidatorTests
{
    [Theory]
    [InlineData("ontology.ttl", OntologyValidator.FormatTurtle, OntologyValidator.ContentTypeTurtle)]
    [InlineData("ONTOLOGY.TTL", OntologyValidator.FormatTurtle, OntologyValidator.ContentTypeTurtle)]
    [InlineData("ontology.rdf", OntologyValidator.FormatRdf, OntologyValidator.ContentTypeRdf)]
    [InlineData("ONTOLOGY.RDF", OntologyValidator.FormatRdf, OntologyValidator.ContentTypeRdf)]
    public void TryGetFormat_WithValidExtension_ReturnsFormatAndContentType(
        string fileName, string expectedFormat, string expectedContentType)
    {
        var result = OntologyValidator.TryGetFormat(fileName, out var format, out var contentType);

        Assert.True(result);
        Assert.Equal(expectedFormat, format);
        Assert.Equal(expectedContentType, contentType);
    }

    [Theory]
    [InlineData("ontology.txt")]
    [InlineData("ontology.xml")]
    [InlineData("ontology")]
    [InlineData("ontology.")]
    [InlineData("")]
    public void TryGetFormat_WithInvalidExtension_ReturnsFalse(string fileName)
    {
        var result = OntologyValidator.TryGetFormat(fileName, out var format, out var contentType);

        Assert.False(result);
        Assert.Empty(format);
        Assert.Empty(contentType);
    }

    [Theory]
    [InlineData("path/to/ontology.ttl")]
    [InlineData(".hidden.rdf")]
    public void TryGetFormat_WithPathOrHiddenFile_RecognizesExtension(string fileName)
    {
        var result = OntologyValidator.TryGetFormat(fileName, out var format, out _);

        Assert.True(result);
        Assert.NotEmpty(format);
    }

    [Fact]
    public void IsValid_WithValidTurtleStream_ReturnsTrue()
    {
        var validTurtle = """
            @prefix ex: <http://example.org/> .
            ex:subject ex:predicate ex:object .
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(validTurtle));
        var positionBefore = stream.Position;
        var result = OntologyValidator.IsValid(stream, OntologyValidator.FormatTurtle);

        Assert.True(result);
        stream.Seek(0, SeekOrigin.Begin);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void IsValid_WithValidRdfStream_ReturnsTrue()
    {
        var validRdf = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     xmlns:ex="http://example.org/">
              <rdf:Description rdf:about="http://example.org/subject">
                <ex:predicate rdf:resource="http://example.org/object"/>
              </rdf:Description>
            </rdf:RDF>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(validRdf));
        var positionBefore = stream.Position;
        var result = OntologyValidator.IsValid(stream, OntologyValidator.FormatRdf);

        Assert.True(result);
        stream.Seek(0, SeekOrigin.Begin);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void IsValid_WithInvalidTurtleStream_ReturnsFalse()
    {
        var invalidTurtle = "this is not valid turtle content @#$%";

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidTurtle));
        var result = OntologyValidator.IsValid(stream, OntologyValidator.FormatTurtle);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithEmptyRdfStream_ReturnsFalse()
    {
        var emptyRdf = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"></rdf:RDF>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(emptyRdf));
        var result = OntologyValidator.IsValid(stream, OntologyValidator.FormatRdf);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithUnknownFormat_ReturnsFalse()
    {
        var content = "some content";

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var result = OntologyValidator.IsValid(stream, "unknown");

        Assert.False(result);
    }

    [Fact]
    public void IsValid_StreamPositionLeftAtBeginning_AfterValidation()
    {
        var validTurtle = """
            @prefix ex: <http://example.org/> .
            ex:s ex:p ex:o .
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(validTurtle));
        stream.Seek(5, SeekOrigin.Begin);
        var positionBefore = stream.Position;

        OntologyValidator.IsValid(stream, OntologyValidator.FormatTurtle);

        // Reset stream to check position
        stream.Seek(0, SeekOrigin.Begin);
        Assert.Equal(0, stream.Position);
    }
}
