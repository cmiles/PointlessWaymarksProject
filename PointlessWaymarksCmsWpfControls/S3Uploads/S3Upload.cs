using System.IO;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    public record S3Upload(FileInfo ToUpload, string S3Key, string BucketName, string Note)
    {
    }
}