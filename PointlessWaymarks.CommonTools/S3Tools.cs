using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using PointlessWaymarks.CommonTools.S3;
using Serilog;

namespace PointlessWaymarks.CommonTools;

public static class S3Tools
{
    /// <summary>
    ///     Lists S3 Items including this programs common metadata
    /// </summary>
    /// <param name="accountInfo"></param>
    /// <param name="prefix">
    ///     Only items whose key starts with this will be included - bucket
    ///     does not start with // (or /) and if you don't end this value with a / the starting
    ///     directory will be included in the return list
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<List<S3RemoteFileAndMetadata>> ListS3Items(IS3AccountInformation accountInfo,
        string prefix, IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        Log.ForContext(nameof(prefix), progress).ForContext(nameof(accountInfo.BucketName), accountInfo.BucketName()).ForContext(nameof(accountInfo.BucketRegion), accountInfo.BucketRegion()).Information("S3 Object Listing - Starting");

        var s3Client = accountInfo.S3Client();

        var listRequest = new ListObjectsV2Request { BucketName = accountInfo.BucketName(), Prefix = prefix };

        var awsObjects = new List<S3RemoteFileAndMetadata>();

        var paginator = s3Client.Paginators.ListObjectsV2(listRequest);

        await foreach (var response in paginator.S3Objects)
        {
            if (awsObjects.Count % 100 == 0)
                progress?.Report($"S3 Object Listing - Added {awsObjects.Count} S3 Objects so far...");

            if (response.Key == prefix) continue;

            awsObjects.Add(await RemoteFileAndMetadata(s3Client, response));
        }

        return awsObjects;
    }

    public static async Task<S3LocalFileAndMetadata> LocalFileAndMetadata(FileInfo localFile)
    {
        return new S3LocalFileAndMetadata(localFile, await LocalFileMetadata(localFile));
    }

    public static async Task<S3LocalFileAndMetadata> LocalFileAndMetadata(string localFile)
    {
        var localFileInfo = new FileInfo(localFile);

        return await LocalFileAndMetadata(localFileInfo);
    }

    public static Task<S3UploadMetadata> LocalFileMetadata(FileInfo localFile)
    {
        return Task.FromResult(new S3UploadMetadata(localFile.LastWriteTimeUtc.ToString("O"), localFile.CalculateMD5()));
    }

    public static async Task<S3StandardMetadata> MetadataFromS3Object(AmazonS3Client client, S3Object s3Object)
    {
        var metadata = await client.GetObjectMetadataAsync(s3Object.BucketName, s3Object.Key);

        var lastWriteTime = metadata.Metadata.Keys.Any(x => x.Equals(S3UploadMetadata.LastWriteTimeKey))
            ? metadata.Metadata[S3UploadMetadata.LastWriteTimeKey] ?? string.Empty
            : string.Empty;
        var fileSystemHash = metadata.Metadata.Keys.Any(x => x.Equals(S3UploadMetadata.FileSystemHashKey))
            ? metadata.Metadata[S3UploadMetadata.FileSystemHashKey] ?? string.Empty
            : string.Empty;

        return new S3StandardMetadata(lastWriteTime, fileSystemHash, metadata.ContentLength);
    }

    public static async Task<S3RemoteFileAndMetadata> RemoteFileAndMetadata(AmazonS3Client client,
        S3Object s3Object)
    {
        return new S3RemoteFileAndMetadata(s3Object.BucketName, s3Object.Key,
            await MetadataFromS3Object(client, s3Object));
    }

    public static async Task<TransferUtilityUploadRequest> S3TransferUploadRequest(FileInfo toUpload, string bucket,
        string key)
    {
        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = bucket,
            FilePath = toUpload.FullName,
            Key = key
        };

        var fileMetadata = await LocalFileMetadata(toUpload);

        return fileMetadata.AddMetadataToRequest(uploadRequest);
    }

    public static async Task<S3UploadRequest> UploadRequest(FileInfo toUpload, string s3Key, string bucketName,
        string region, string note)
    {
        return new S3UploadRequest(await LocalFileAndMetadata(toUpload), s3Key, bucketName, region, note);
    }
}