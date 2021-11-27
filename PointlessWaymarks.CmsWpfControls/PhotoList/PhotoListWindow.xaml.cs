using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

/// <summary>
///     Interaction logic for PhotoListWindow.xaml
/// </summary>
[ObservableObject]
public partial class PhotoListWindow
{
    [ObservableProperty] private PhotoListWithActionsContext _photoListContext;
    [ObservableProperty] private string _windowTitle = "Photo List";

    public PhotoListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}