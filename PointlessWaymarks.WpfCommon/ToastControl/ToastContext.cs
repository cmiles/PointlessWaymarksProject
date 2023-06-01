using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.ToastControl;

[NotifyPropertyChanged]
public partial class ToastContext
{
    public Action? InvokeHideAnimation;

    public ToastContext()
    {
        Id = Guid.NewGuid();
        CreateTime = DateTime.Now;
    }

    public DateTime CreateTime { get; }
    public Guid Id { get; }
    public string? Message { get; set; } = "";
    public ToastType Type { get; set; }
}