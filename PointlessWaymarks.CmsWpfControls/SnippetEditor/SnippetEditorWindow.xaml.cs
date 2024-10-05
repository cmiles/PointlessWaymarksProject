using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.SnippetEditor;

/// <summary>
///     Interaction logic for SnippetEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class SnippetEditorWindow
{
    private SnippetEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"Snippet Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public SnippetEditorContext? Snippet { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<SnippetEditorWindow> CreateInstance(Snippet? toLoad = null,
        bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new SnippetEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.Snippet = await SnippetEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.Snippet.TitleEntry.PropertyChanged += (_, _) =>
        {
            window.WindowTitle =
                $"Snippet Editor - {UserSettingsSingleton.CurrentSettings().SiteName} - {window.Snippet.TitleEntry.UserValue}";
        };

        window.Snippet.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.Snippet);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}