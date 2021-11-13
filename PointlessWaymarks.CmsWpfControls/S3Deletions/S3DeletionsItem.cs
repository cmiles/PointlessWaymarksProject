#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.S3Deletions;

public class S3DeletionsItem : INotifyPropertyChanged
{
    private string _amazonObjectKey = string.Empty;
    private string _bucketName = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public string AmazonObjectKey
    {
        get => _amazonObjectKey;
        set
        {
            if (value == _amazonObjectKey) return;
            _amazonObjectKey = value;
            OnPropertyChanged();
        }
    }

    public string BucketName
    {
        get => _bucketName;
        set
        {
            if (value == _bucketName) return;
            _bucketName = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (value == _errorMessage) return;
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            if (value == _hasError) return;
            _hasError = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}