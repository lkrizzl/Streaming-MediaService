using Amazon.S3;
using Amazon.S3.Model;
using MediaService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MediaService.Infrastructure.Storage;

public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _client;
    private readonly IAmazonS3 _publicClient;
    private readonly MinioOptions _options;

    public MinioStorageService(IAmazonS3 client, [FromKeyedServices("public")] IAmazonS3 publicClient, IOptions<MinioOptions> options)
    {
        _client = client;
        _publicClient = publicClient;
        _options = options.Value;
    }

    public Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        return Task.FromResult(_publicClient.GetPreSignedURL(request));
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var objectKey = $"{Guid.NewGuid()}_{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = contentType
        };

        await _client.PutObjectAsync(request, cancellationToken);

        return objectKey;
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(_options.Bucket, objectKey, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_options.Bucket, objectKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var buckets = await _client.ListBucketsAsync(cancellationToken);
        var exists = buckets.Buckets.Any(b => b.BucketName == _options.Bucket);

        if (!exists)
        {
            await _client.PutBucketAsync(_options.Bucket, cancellationToken);
        }
    }


}