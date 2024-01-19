using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.ToastControl;

[NotifyPropertyChanged]
public partial class ToastContext
{
    public Action? InvokeHideAnimation;

    public DateTime CreateTime { get; } = DateTime.Now;
    public Guid Id { get; } = Guid.NewGuid();
    public string? Message { get; set; } = "";
    public ToastType Type { get; set; }
}