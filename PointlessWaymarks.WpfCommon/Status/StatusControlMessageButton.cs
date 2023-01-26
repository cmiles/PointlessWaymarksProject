using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.Status;

public partial class StatusControlMessageButton : ObservableObject
{
    [ObservableProperty] private bool _isDefault;
    [ObservableProperty] private string _messageText;
}