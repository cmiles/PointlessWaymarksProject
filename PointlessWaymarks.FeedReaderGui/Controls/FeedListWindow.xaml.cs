using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
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

    public static async Task<FeedListWindow> CreateInstance(string dbFile)
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

        window.FeedContext = await FeedListContext.CreateInstance(window.StatusContext, dbFile);
        
        window.StatusContext.BlockUi = false;
        
        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}