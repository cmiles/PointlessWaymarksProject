using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.NoteList;

/// <summary>
///     Interaction logic for NoteListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class NoteListWindow
{
    private NoteListWindow(NoteListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public NoteListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Note List";

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<NoteListWindow> CreateInstance(NoteListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new NoteListWindow(toLoad ?? await NoteListWithActionsContext.CreateInstance(null));

        return window;
    }
}