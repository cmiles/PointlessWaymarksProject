using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

/// <summary>
///     Interaction logic for GeoJsonListWindow.xaml
/// </summary>
[ObservableObject]
public partial class GeoJsonListWindow
{
    [ObservableProperty] private GeoJsonListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "GeoJson List";

    public GeoJsonListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}