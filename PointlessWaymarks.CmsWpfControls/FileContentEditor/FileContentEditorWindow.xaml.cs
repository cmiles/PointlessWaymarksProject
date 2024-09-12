using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileContentEditor;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class FileContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private FileContentEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"File Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public FileContentEditorContext? FileContent { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileContentEditorWindow> CreateInstance(bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.FileContent = await FileContentEditorContext.CreateInstance(window.StatusContext);

        window.WindowTitle =
            $"File Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.FileContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.FileContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"File Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.FileContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.FileContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.FileContent)
            {
                CloseAction = x => { ((FileContentEditorWindow)x).FileContent?.MainImageExternalEditorWindowCleanup(); }
            };

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileContentEditorWindow> CreateInstance(FileInfo initialFile, bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.FileContent = await FileContentEditorContext.CreateInstance(window.StatusContext, initialFile);

        window.WindowTitle =
            $"File Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.FileContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.FileContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, args) =>
        {
            window.WindowTitle =
                $"File Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.FileContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.FileContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.FileContent)
            {
                CloseAction = x => { ((FileContentEditorWindow)x).FileContent?.MainImageExternalEditorWindowCleanup(); }
            };

        return window;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileContentEditorWindow> CreateInstance(FileContent toLoad, bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.FileContent = await FileContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.WindowTitle =
            $"File Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.FileContent.TitleSummarySlugFolder.TitleEntry.UserValue}";

        window.FileContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, args) =>
        {
            window.WindowTitle =
                $"File Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.FileContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.FileContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.FileContent)
            {
                CloseAction = x => { ((FileContentEditorWindow)x).FileContent?.MainImageExternalEditorWindowCleanup(); }
            };

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}