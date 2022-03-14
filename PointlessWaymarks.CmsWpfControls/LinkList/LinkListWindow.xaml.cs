using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

/// <summary>
///     Interaction logic for LinkListWindow.xaml
/// </summary>
[ObservableObject]
public partial class LinkListWindow
{
    [ObservableProperty] private LinkListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Link List";

    public LinkListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}