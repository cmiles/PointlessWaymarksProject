using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.Status;

[ObservableObject]
public partial class StatusControlMessageButton
{
    [ObservableProperty] private bool _isDefault;
    [ObservableProperty] private string _messageText;
}