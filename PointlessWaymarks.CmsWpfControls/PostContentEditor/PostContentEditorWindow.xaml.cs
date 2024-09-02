using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.PostContentEditor;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class PostContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private PostContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
        WindowTitle = $"Post Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public PostContentEditorContext? PostContent { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<PostContentEditorWindow> CreateInstance(PostContent? toLoad = null)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PostContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PostContent = await PostContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.PostContent.TitleSummarySlugFolder.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Post Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.PostContent.TitleSummarySlugFolder.TitleEntry.UserValue}";
        };

        window.PostContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PostContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}