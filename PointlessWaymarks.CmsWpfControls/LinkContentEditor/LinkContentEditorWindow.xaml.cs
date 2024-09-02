using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.LinkContentEditor;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class LinkContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private LinkContentEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"Link Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public LinkContentEditorContext? LinkContent { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<LinkContentEditorWindow> CreateInstance(LinkContent? toLoad,
        bool extractDataFromLink = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LinkContentEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.LinkContent =
            await LinkContentEditorContext.CreateInstance(window.StatusContext, toLoad, extractDataFromLink);

        window.LinkContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.LinkContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}