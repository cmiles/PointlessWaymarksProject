using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
public partial class FeedItemListWindow
{
    public FeedItemListWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public FeedItemListContext? ItemsContext { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public static async Task<FeedItemListWindow> CreateInstance(List<Guid>? feedList = null)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FeedItemListWindow
        {
            StatusContext =
            {
                BlockUi = true
            }
        };

        window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.StatusContext.Progress("Feed Items List - Creating Context");

        window.ItemsContext = await FeedItemListContext.CreateInstance(window.StatusContext, feedList);

        window.StatusContext.BlockUi = false;

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}