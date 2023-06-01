using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.S3Deletions;

[NotifyPropertyChanged]
public partial class S3DeletionsItem
{
    public string AmazonObjectKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool HasError { get; set; }
}