#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.S3Deletions;

public partial class S3DeletionsItem : ObservableObject
{
    [ObservableProperty] private string _amazonObjectKey = string.Empty;
    [ObservableProperty] private string _bucketName = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
}