using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.SimpleMediaPlayer
{
    public partial class SimpleMediaPlayerContext : ObservableObject
    {
        [ObservableProperty] private string _videoSource = string.Empty;
        [ObservableProperty] private double _videoPositionInMilliseconds = 0D;
    }
}
