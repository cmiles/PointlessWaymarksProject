using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

/// <summary>
///     Interaction logic for MapComponentEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MapComponentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private MapComponentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public MapComponentEditorContext? MapComponentContent { get; set; }
    public StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<MapComponentEditorWindow> CreateInstance(MapComponent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new MapComponentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.MapComponentContent = await MapComponentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.MapComponentContent.RequestContentEditorWindowClose += (_, _) =>
        {
            window.Dispatcher?.Invoke(window.Close);
        };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.MapComponentContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}