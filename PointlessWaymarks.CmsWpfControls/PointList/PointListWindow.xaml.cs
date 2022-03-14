using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;

namespace PointlessWaymarks.CmsWpfControls.PointList;

/// <summary>
///     Interaction logic for PointListWindow.xaml
/// </summary>
[ObservableObject]
public partial class PointListWindow
{
    [ObservableProperty] private PointListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Point List";

    public PointListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}