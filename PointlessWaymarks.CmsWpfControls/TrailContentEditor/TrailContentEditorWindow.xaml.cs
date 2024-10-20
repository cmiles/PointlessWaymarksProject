using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.TrailContentEditor;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class TrailContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private TrailContentEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"Trail Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public TrailContentEditorContext? TrailContent { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<TrailContentEditorWindow> CreateInstance(TrailContent? toLoad = null, bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new TrailContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.TrailContent = await TrailContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.TrailContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Trail Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.TrailContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.TrailContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.TrailContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}