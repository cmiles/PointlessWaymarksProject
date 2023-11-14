using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;

/// <summary>
///     Interaction logic for GeoJsonContentEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class GeoJsonContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private GeoJsonContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public GeoJsonContentEditorContext? GeoJsonContent { get; set; }
    public StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<GeoJsonContentEditorWindow> CreateInstance(GeoJsonContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new GeoJsonContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.GeoJsonContent = await GeoJsonContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.GeoJsonContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.GeoJsonContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}