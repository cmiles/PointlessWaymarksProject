using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.LineList;

/// <summary>
///     Interaction logic for LineListWindow.xaml
/// </summary>
[ObservableObject]
public partial class LineListWindow
{
    [ObservableProperty] private LineListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Line List";

    public LineListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}