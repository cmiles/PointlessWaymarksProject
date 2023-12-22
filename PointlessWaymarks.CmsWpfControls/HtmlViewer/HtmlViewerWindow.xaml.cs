using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.HtmlViewer;

public partial class HtmlViewerWindow
{
    private HtmlViewerWindow()
    {
        InitializeComponent();
        Title = $"Html Viewer - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<HtmlViewerWindow> CreateInstance(string htmlString)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new HtmlViewerWindow
        {
            DataContext = new HtmlViewerContext { HtmlString = htmlString }
        };

        return window;
    }
}