using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.ToastControl;

public partial class ToastContext : ObservableObject
{
    [ObservableProperty] private string? _message = "";
    [ObservableProperty] private ToastType _type;

    public Action? InvokeHideAnimation;

    public ToastContext()
    {
        Id = Guid.NewGuid();
        CreateTime = DateTime.Now;
    }

    public DateTime CreateTime { get; }
    public Guid Id { get; }
}