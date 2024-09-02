using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.LineContentEditor;

/// <summary>
///     Interaction logic for LineContentEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class LineContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private LineContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
        WindowTitle = $"Line Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public LineContentEditorContext? LineContent { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<LineContentEditorWindow> CreateInstance(LineContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LineContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.LineContent = await LineContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.WindowTitle =
            $"Line Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.LineContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.LineContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Line Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.LineContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.LineContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.LineContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}