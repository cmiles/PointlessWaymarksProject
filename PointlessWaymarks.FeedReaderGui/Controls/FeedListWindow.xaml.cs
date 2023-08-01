using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
public partial class FeedListWindow
{
    public FeedListWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public FeedListContext? FeedContext { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public static async Task<FeedListWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FeedListWindow
        {
            StatusContext =
            {
                BlockUi = true
            }
        };

        window.PositionWindowAndShow();
        
        await ThreadSwitcher.ResumeBackgroundAsync();

        window.StatusContext.Progress("Feed List - Creating Context");

        window.FeedContext = await FeedListContext.CreateInstance(window.StatusContext);
        
        window.StatusContext.BlockUi = false;
        
        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}