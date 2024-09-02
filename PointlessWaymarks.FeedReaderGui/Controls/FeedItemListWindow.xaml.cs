using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class FeedItemListWindow
{
    public FeedItemListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public FeedItemListContext? ItemsContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<FeedItemListWindow> CreateInstance(string dbFile, List<Guid>? feedList = null,
        bool showUnread = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = await StatusControlContext.CreateInstance();
        factoryStatusContext.BlockUi = true;

        var window = new FeedItemListWindow
        {
            StatusContext = factoryStatusContext
        };

        window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.StatusContext.Progress("Feed Items List - Creating Context");

        window.ItemsContext =
            await FeedItemListContext.CreateInstance(window.StatusContext, dbFile, feedList, showUnread);

        window.StatusContext.BlockUi = false;

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}