using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PostList;

/// <summary>
///     Interaction logic for PostListWindow.xaml
/// </summary>
[ObservableObject]
public partial class PostListWindow
{
    [ObservableProperty] private PostListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Post List";

    private PostListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<PostListWindow> CreateInstance(PostListWithActionsContext toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new PostListWindow
        {
            ListContext = toLoad ?? new PostListWithActionsContext(null)
        };

        return window;
    }
}