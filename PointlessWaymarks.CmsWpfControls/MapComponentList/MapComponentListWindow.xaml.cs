using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

/// <summary>
///     Interaction logic for MapComponentListWindow.xaml
/// </summary>
[ObservableObject]
public partial class MapComponentListWindow
{
    [ObservableProperty] private MapComponentListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Map List";

    public MapComponentListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}