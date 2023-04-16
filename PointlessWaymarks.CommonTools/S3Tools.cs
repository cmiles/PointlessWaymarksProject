using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CommonTools;

public static class S3Tools
{
    public static async Task<List<S3RemoteFileAndMetadata>> ListS3Items(S3AccountInformation accountInfo,
        IProgress<string>? progress = null)
    {
        var s3Client = S3Client(accountInfo);

        var listRequest = new ListObjectsV2Request { BucketName = accountInfo.BucketName() };

        var awsObjects = new List<S3RemoteFileAndMetadata>();

        ListObjectsV2Response listResponse;

        var loopNumber = 0;

        do
        {
            progress?.Report($"Aws Object Listing Loop {++loopNumber}");

            listResponse = await s3Client.ListObjectsV2Async(listRequest);

            progress?.Report($"Adding {listResponse.S3Objects.Count} S3 Objects to List...");

            foreach (var x in listResponse.S3Objects) awsObjects.Add(await RemoteFileAndMetadata(s3Client, x));

            // Set the marker property
            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        } while (listResponse.IsTruncated);

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

    public static Task<S3Metadata> LocalFileMetadata(FileInfo localFile)
    {
        return Task.FromResult(new S3Metadata(localFile.LastWriteTimeUtc.ToString("O"), localFile.CalculateMD5()));
    }

    public static async Task<S3Metadata> MetadataFromS3Object(AmazonS3Client client, S3Object s3Object)
    {
        var metadata = await client.GetObjectMetadataAsync(s3Object.BucketName, s3Object.Key);

        var lastWriteTime = metadata.Metadata.Keys.Any(x => x.Equals(S3Metadata.LastWriteTimeKey))
            ? metadata.Metadata[S3Metadata.LastWriteTimeKey] ?? string.Empty
            : string.Empty;
        var fileSystemHash = metadata.Metadata.Keys.Any(x => x.Equals(S3Metadata.FileSystemHashKey))
            ? metadata.Metadata[S3Metadata.FileSystemHashKey] ?? string.Empty
            : string.Empty;

        return new S3Metadata(lastWriteTime, fileSystemHash);
    }

    public static async Task<S3RemoteFileAndMetadata> RemoteFileAndMetadata(AmazonS3Client client,
        S3Object s3Object)
    {
        return new S3RemoteFileAndMetadata(s3Object.BucketName, s3Object.Key,
            await MetadataFromS3Object(client, s3Object));
    }

    public static AmazonS3Client S3Client(S3AccountInformation accountInfo)
    {
        var bucketRegion = accountInfo.BucketRegion();
        var accessKey = accountInfo.AccessKey();
        var secret = accountInfo.Secret();

        return new AmazonS3Client(accessKey, secret);
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