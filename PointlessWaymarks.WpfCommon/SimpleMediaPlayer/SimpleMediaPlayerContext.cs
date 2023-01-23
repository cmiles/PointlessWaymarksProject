using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.SimpleMediaPlayer
{
    [ObservableObject]
    public partial class SimpleMediaPlayerContext
    {
        [ObservableProperty] private string _videoSource;
        [ObservableProperty] private double _videoPositionInMilliseconds;
    }
}
