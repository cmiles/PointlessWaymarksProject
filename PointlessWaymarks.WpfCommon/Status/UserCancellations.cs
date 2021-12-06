using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Commands;

namespace PointlessWaymarks.WpfCommon.Status;

[ObservableObject]
public partial class UserCancellations
{
    [ObservableProperty] private Command _cancel;
    [ObservableProperty] private CancellationTokenSource _cancelSource;
    [ObservableProperty] private string _description;
    [ObservableProperty] private bool _isEnabled = true;

    public UserCancellations()
    {
        Cancel = new Command(() =>
        {
            CancelSource?.Cancel();
            IsEnabled = false;
            Description = "Canceling...";
        });
    }
}