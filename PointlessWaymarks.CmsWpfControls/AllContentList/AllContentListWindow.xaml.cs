using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.AllContentList;

/// <summary>
///     Interaction logic for AllItemsWithActionsWindow.xaml
/// </summary>
[ObservableObject]
public partial class AllContentListWindow
{
    [ObservableProperty] private AllContentListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "All Content List";

    public AllContentListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}