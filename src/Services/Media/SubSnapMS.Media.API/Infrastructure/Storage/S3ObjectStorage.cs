using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace SubSnapMS.Media.API.Infrastructure.Storage;

internal sealed class S3ObjectStorage(IAmazonS3 s3, IOptions<ObjectStorageOptions> options) : IObjectStorage
{
    private ObjectStorageOptions Settings => options.Value;

    public string CreateUploadUrl(string objectKey, string contentType, DateTimeOffset expiresAtUtc) =>
        s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = Settings.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAtUtc.UtcDateTime,
            Protocol = Settings.UsesHttps ? Protocol.HTTPS : Protocol.HTTP
        });

    public async Task<StoredObjectMetadata?> GetMetadataAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await s3.GetObjectMetadataAsync(Settings.BucketName, objectKey, cancellationToken);
            return new StoredObjectMetadata(metadata.ContentLength, metadata.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        await s3.DeleteObjectAsync(Settings.BucketName, objectKey, cancellationToken);

    public string GetPublicUrl(string objectKey) =>
        $"{Settings.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
}