using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

[NotifyPropertyChanged]
public partial class PhotoContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private PhotoContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
        WindowTitle = $"Photo Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public PhotoContentEditorContext? PhotoContent { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<PhotoContentEditorWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PhotoContent = await PhotoContentEditorContext.CreateInstance(window.StatusContext);

        window.WindowTitle =
            $"Photo Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.PhotoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.PhotoContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, args) =>
        {
            window.WindowTitle =
                $"Photo Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.PhotoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.PhotoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PhotoContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<PhotoContentEditorWindow> CreateInstance(FileInfo initialPhoto)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PhotoContent = await PhotoContentEditorContext.CreateInstance(window.StatusContext, initialPhoto);

        window.WindowTitle =
            $"Photo Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.PhotoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.PhotoContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, args) =>
        {
            window.WindowTitle =
                $"Photo Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.PhotoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.PhotoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PhotoContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<PhotoContentEditorWindow> CreateInstance(PhotoContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PhotoContent = await PhotoContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.WindowTitle =
            $"Photo Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.PhotoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.PhotoContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Photo Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.PhotoContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.PhotoContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PhotoContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}