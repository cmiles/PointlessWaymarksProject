using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.ToastControl;

public partial class ToastViewModel : ObservableObject
{
    [ObservableProperty] private string? _message = "";
    [ObservableProperty] private ToastType _type;

    public Action InvokeHideAnimation;

    public ToastViewModel()
    {
        Id = Guid.NewGuid();
        CreateTime = DateTime.Now;
    }

    public DateTime CreateTime { get; }
    public Guid Id { get; }
}