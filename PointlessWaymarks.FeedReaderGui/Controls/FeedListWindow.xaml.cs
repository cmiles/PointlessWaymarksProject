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
        DataContext = this;
    }

    public FeedListContext? FeedContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<FeedListWindow> CreateInstance(string dbFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = await StatusControlContext.CreateInstance();
        factoryStatusContext.BlockUi = true;

        var window = new FeedListWindow
        {
            StatusContext = factoryStatusContext
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