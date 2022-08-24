using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.NoteList;

/// <summary>
///     Interaction logic for NoteListWindow.xaml
/// </summary>
[ObservableObject]
public partial class NoteListWindow
{
    [ObservableProperty] private NoteListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Note List";

    private NoteListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<NoteListWindow> CreateInstance(NoteListWithActionsContext toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new NoteListWindow
        {
            ListContext = toLoad ?? new NoteListWithActionsContext(null)
        };

        return window;
    }
}