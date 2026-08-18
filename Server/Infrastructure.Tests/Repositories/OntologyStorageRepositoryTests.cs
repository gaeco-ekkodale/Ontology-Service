// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Net;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.Exceptions;
using NSubstitute;
using OntologyService.Infrastructure.Repositories;

namespace OntologyService.Infrastructure.Tests.Repositories;

public class OntologyStorageRepositoryTests
{
	private readonly IMinioClient _minioClient;
	private readonly OntologyStorageRepository _repo;

	public OntologyStorageRepositoryTests()
	{
		_minioClient = Substitute.For<IMinioClient>();
		_repo = new OntologyStorageRepository(_minioClient);
	}

	[Fact]
	public async Task UploadAsync_WithExistingBucket_UploadsFile()
	{
		var bucketName = "ontology";
		var objectKey = "ttl/somefile.ttl";
		var contentType = "text/turtle";
		var fileContent = "@prefix ex: <http://example.org/> . ex:s ex:p ex:o .";

		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

		_minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
			.Returns(true);

		_minioClient.PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>())
			.Returns(new PutObjectResponse(HttpStatusCode.OK, bucketName, new Dictionary<string, string>(), 0, objectKey) { Etag = "abc123" });

		var result = await _repo.UploadAsync(bucketName, objectKey, stream, contentType);

		Assert.Equal(objectKey, result.ObjectName);
		Assert.Equal("abc123", result.Etag);

		await _minioClient.DidNotReceive().MakeBucketAsync(Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>());
		await _minioClient.Received(1).PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UploadAsync_WithNonExistentBucket_CreatesBucketThenUploads()
	{
		var bucketName = "ontology";
		var objectKey = "ttl/somefile.ttl";
		var contentType = "text/turtle";
		var fileContent = "@prefix ex: <http://example.org/> . ex:s ex:p ex:o .";

		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

		_minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
			.Returns(false);

		_minioClient.MakeBucketAsync(Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		_minioClient.PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>())
			.Returns(new PutObjectResponse(HttpStatusCode.OK, bucketName, new Dictionary<string, string>(), 0, objectKey) { Etag = "abc123" });

		var result = await _repo.UploadAsync(bucketName, objectKey, stream, contentType);

		Assert.Equal(objectKey, result.ObjectName);

		await _minioClient.Received(1).MakeBucketAsync(Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>());
		await _minioClient.Received(1).PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UploadAsync_WhenPutObjectReturnsNullObjectName_ThrowsException()
	{
		var bucketName = "ontology";
		var objectKey = "ttl/somefile.ttl";
		var contentType = "text/turtle";

		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));

		_minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
			.Returns(true);

		_minioClient.PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>())
			.Returns(new PutObjectResponse(HttpStatusCode.OK, bucketName, new Dictionary<string, string>(), 0, null));

		var ex = await Assert.ThrowsAsync<Exception>(() => _repo.UploadAsync(bucketName, objectKey, stream, contentType));
		Assert.Contains("ObjectName is null or empty", ex.Message);
	}

	[Fact]
	public async Task UploadAsync_WhenMinioThrowsException_RethrowsAsException()
	{
		var bucketName = "ontology";
		var objectKey = "ttl/somefile.ttl";
		var contentType = "text/turtle";

		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));

		_minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
			.Returns(true);

		_minioClient.PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<PutObjectResponse>(new MinioException("Connection failed")));

		var ex = await Assert.ThrowsAsync<Exception>(() => _repo.UploadAsync(bucketName, objectKey, stream, contentType));
		Assert.Contains("Fehler beim Hochladen", ex.Message);
	}

	[Fact]
	public async Task RemoveAsync_WithValidBucketAndKey_RemovesObject()
	{
		var bucketName = "ontology";
		var objectKey = "ttl/somefile.ttl";

		_minioClient.RemoveObjectAsync(Arg.Any<RemoveObjectArgs>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		await _repo.RemoveAsync(bucketName, objectKey);

		await _minioClient.Received(1).RemoveObjectAsync(Arg.Any<RemoveObjectArgs>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task RemoveAsync_WhenMinioThrowsException_RethrowsAsException()
	{
		var bucketName = "ontology";
		var objectKey = "ttl/somefile.ttl";

		_minioClient.RemoveObjectAsync(Arg.Any<RemoveObjectArgs>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException(new MinioException("Access denied")));

		var ex = await Assert.ThrowsAsync<Exception>(() => _repo.RemoveAsync(bucketName, objectKey));
		Assert.Contains("Fehler beim Löschen", ex.Message);
	}
}
