using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.FileList;

/// <summary>
///     Interaction logic for FileListWindow.xaml
/// </summary>
[ObservableObject]
public partial class FileListWindow
{
    [ObservableProperty] private FileListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Files List";

    public FileListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}