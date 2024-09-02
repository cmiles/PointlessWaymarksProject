using System.Windows;
using PointlessWaymarks.FeedReaderData.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.FeedReaderGui.Controls;

/// <summary>
///     Interaction logic for FeedEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class FeedEditorWindow : Window
{
    public FeedEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public FeedEditorContext? FeedContext { get; set; }
    public StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<FeedEditorWindow> CreateInstance(ReaderFeed toLoad, string dbFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FeedEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.FeedContext = await FeedEditorContext.CreateInstance(window.StatusContext, toLoad, dbFile);

        window.FeedContext.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.FeedContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}