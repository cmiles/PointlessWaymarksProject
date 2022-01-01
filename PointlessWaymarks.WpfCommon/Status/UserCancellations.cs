using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace PointlessWaymarks.WpfCommon.Status;

[ObservableObject]
public partial class UserCancellations
{
    [ObservableProperty] private RelayCommand _cancel;
    [ObservableProperty] private CancellationTokenSource _cancelSource;
    [ObservableProperty] private string _description;
    [ObservableProperty] private bool _isEnabled = true;

    public UserCancellations()
    {
        Cancel = new RelayCommand(() =>
        {
            CancelSource?.Cancel();
            IsEnabled = false;
            Description = "Canceling...";
        });
    }
}