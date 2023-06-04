using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

/// <summary>
///     Interaction logic for LinkListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class LinkListWindow
{
    private LinkListWindow(LinkListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public LinkListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Link List";

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<LinkListWindow> CreateInstance(LinkListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LinkListWindow(toLoad ?? await LinkListWithActionsContext.CreateInstance(null));

        return window;
    }
}