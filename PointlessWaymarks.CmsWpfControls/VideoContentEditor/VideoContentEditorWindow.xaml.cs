using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.VideoContentEditor;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class VideoContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private VideoContentEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"Video Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public VideoContentEditorContext? VideoContent { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoContentEditorWindow> CreateInstance(bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new VideoContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext);

        window.WindowTitle =
            $"Video Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.VideoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.VideoContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Video Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.VideoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
            {
                CloseAction = x =>
                {
                    var videoContent = x as VideoContentEditorWindow;
                    videoContent?.VideoContent?.MainImageExternalEditorWindowCleanup();
                }
            };

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoContentEditorWindow> CreateInstance(FileInfo initialFile, bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new VideoContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext, initialFile);

        window.WindowTitle =
            $"Video Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.VideoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.VideoContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Video Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.VideoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
            {
                CloseAction = x =>
                {
                    var videoContent = x as VideoContentEditorWindow;
                    videoContent?.VideoContent?.MainImageExternalEditorWindowCleanup();
                }
            };

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoContentEditorWindow> CreateInstance(VideoContent? toLoad, bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new VideoContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.VideoContent = await VideoContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.WindowTitle =
            $"Video Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.VideoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.VideoContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Video Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.VideoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.VideoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.VideoContent)
            {
                CloseAction = x =>
                {
                    var videoContent = x as VideoContentEditorWindow;
                    videoContent?.VideoContent?.MainImageExternalEditorWindowCleanup();
                }
            };

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}