using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.Server;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

/// <summary>
///     Interaction logic for SiteOnDiskPreviewWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class SiteOnDiskPreviewWindow
{
    private static PreviewServer? _previewServer;

    private SiteOnDiskPreviewWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public SitePreviewContext? PreviewContext { get; set; }
    public StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<SiteOnDiskPreviewWindow> CreateInstance(string initialUrl = "")
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new SiteOnDiskPreviewWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (_previewServer == null)
        {
            _previewServer = new PreviewServer();

            window.StatusContext.RunFireAndForgetWithToastOnError(async () =>
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                await _previewServer.StartServer(UserSettingsSingleton.CurrentSettings().SiteDomainName,
                    UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName);
            });
        }

        window.PreviewContext = new SitePreviewContext(UserSettingsSingleton.CurrentSettings().SiteDomainName,
            UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
            UserSettingsSingleton.CurrentSettings().SiteName, $"localhost:{_previewServer.ServerPort}",
            window.StatusContext,
            initialUrl);

        return window;
    }
}