using System.IO;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    public record S3Upload(FileInfo ToUpload, string S3Key, string BucketName, string Region, string Note)
    {
    }

    public record S3UploadFileRecord(string fileFullName, string S3Key, string BucketName, string Region, string Note)
    {
    }
}