using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.Status;

[NotifyPropertyChanged]
public partial class UserCancellations
{
    public UserCancellations()
    {
        Cancel = new RelayCommand(() =>
        {
            CancelSource?.Cancel();
            IsEnabled = false;
            Description = "Canceling...";
        });
    }

    public RelayCommand? Cancel { get; set; }
    public CancellationTokenSource? CancelSource { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}