using PointlessWaymarks.CmsData;
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
[StaThreadConstructorGuard]
public partial class GeoJsonContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private GeoJsonContentEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"GeoJson Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public GeoJsonContentEditorContext? GeoJsonContent { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<GeoJsonContentEditorWindow> CreateInstance(GeoJsonContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new GeoJsonContentEditorWindow() { StatusContext = await StatusControlContext.CreateInstance() };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.GeoJsonContent = await GeoJsonContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.WindowTitle =
            $"GeoJson Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.GeoJsonContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.GeoJsonContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"GeoJson Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.GeoJsonContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.GeoJsonContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.GeoJsonContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}