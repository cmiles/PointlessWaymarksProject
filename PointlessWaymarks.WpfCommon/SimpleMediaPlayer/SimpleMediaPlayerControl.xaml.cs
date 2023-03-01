using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace PointlessWaymarks.WpfCommon.SimpleMediaPlayer
{
    /// <summary>
    /// Interaction logic for SimpleMediaPlayerControl.xaml
    /// </summary>
    public partial class SimpleMediaPlayerControl
    {
        private DispatcherTimer? _timerVideoTime;

        private bool _userIsDraggingSlider;

        private SimpleMediaPlayerContext? _context;
        

        public SimpleMediaPlayerControl()
        {
            InitializeComponent();
        }

        // Create the timer and otherwise get ready.
        private void ControlLoaded(object sender, RoutedEventArgs e)
        {
            _timerVideoTime = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            _timerVideoTime.Tick += TimerTick;

            VideoContentPlayer.Stop();
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            if (VideoContentPlayer.Source != null && VideoContentPlayer.NaturalDuration.HasTimeSpan &&
                !_userIsDraggingSlider)
                VideoBarPosition.Value = VideoContentPlayer.Position.TotalMilliseconds;
        }

        private void Video_MediaOpened(object? sender, RoutedEventArgs e)
        {
            VideoBarPosition.Minimum = 0;
            VideoBarPosition.Maximum = VideoContentPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            VideoBarPosition.Visibility = Visibility.Visible;
            Debug.Assert(_timerVideoTime != null, nameof(_timerVideoTime) + " != null");
            _timerVideoTime.Start();
        }

        private void VideoPause_Click(object? sender, RoutedEventArgs e)
        {
            VideoContentPlayer.Pause();
        }

        private void VideoPlay_Click(object? sender, RoutedEventArgs e)
        {
            VideoContentPlayer.Play();
        }

        private void VideoProgress_DragCompleted(object? sender, DragCompletedEventArgs e)
        {
            _userIsDraggingSlider = false;
            var sliderValue = (int)VideoBarPosition.Value;
            var ts = new TimeSpan(0, 0, 0, 0, sliderValue);
            VideoContentPlayer.Position = ts;
        }

        private void VideoProgress_DragStarted(object? sender, DragStartedEventArgs e)
        {
            _userIsDraggingSlider = true;
        }

        private void VideoProgress_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoProgressStatus.Text = TimeSpan.FromMilliseconds(VideoBarPosition.Value).ToString(@"hh\:mm\:ss");
            if(_context != null) _context.VideoPositionInMilliseconds = VideoBarPosition.Value;
        }

        private void VideoRestart_Click(object? sender, RoutedEventArgs e)
        {
            VideoContentPlayer.Stop();
            VideoContentPlayer.Play();
        }

        private void VideoStop_Click(object? sender, RoutedEventArgs e)
        {
            VideoContentPlayer.Stop();
        }

        private void SimpleMediaPlayerControl_OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            _context = e.NewValue as SimpleMediaPlayerContext;
        }
    }
}
