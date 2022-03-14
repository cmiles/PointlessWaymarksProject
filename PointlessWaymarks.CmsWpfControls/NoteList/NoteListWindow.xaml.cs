using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.NoteList;

/// <summary>
///     Interaction logic for NoteListWindow.xaml
/// </summary>
[ObservableObject]
public partial class NoteListWindow
{
    [ObservableProperty] private NoteListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Note List";

    public NoteListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }
}