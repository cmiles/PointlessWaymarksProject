using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.PostList;

/// <summary>
///     Interaction logic for PostListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class PostListWindow
{
    private PostListWindow(PostListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public PostListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Post List";

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<PostListWindow> CreateInstance(PostListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new PostListWindow(toLoad ?? await PostListWithActionsContext.CreateInstance(null));

        return window;
    }
}