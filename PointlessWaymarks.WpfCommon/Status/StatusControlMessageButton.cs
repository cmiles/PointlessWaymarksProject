using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.Status;

[NotifyPropertyChanged]
public partial class StatusControlMessageButton
{
    public bool IsDefault { get; set; }
    public string MessageText { get; set; } = string.Empty;
}