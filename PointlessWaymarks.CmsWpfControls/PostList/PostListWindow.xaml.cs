using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.PostList;

/// <summary>
///     Interaction logic for PostListWindow.xaml
/// </summary>
[ObservableObject]
public partial class PostListWindow
{
    [ObservableProperty] private PostListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Post List";

    public PostListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}