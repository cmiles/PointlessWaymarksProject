using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.ImageList;

/// <summary>
///     Interaction logic for ImageListWindow.xaml
/// </summary>
[ObservableObject]
public partial class ImageListWindow
{
    [ObservableProperty] private ImageListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Image List";

    public ImageListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}