using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.SimpleMediaPlayer;

[NotifyPropertyChanged]
public partial class SimpleMediaPlayerContext
{
    public double VideoPositionInMilliseconds { get; set; }
    public string VideoSource { get; set; } = string.Empty;
}