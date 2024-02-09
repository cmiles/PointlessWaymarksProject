using Microsoft.Extensions.Hosting;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

/// <summary>
///     Interaction logic for SiteOnDiskPreviewWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class SiteOnDiskPreviewWindow
{
    private static IHost? _server;
    private static int serverPort;

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

        if (_server == null)
        {
            serverPort = PreviewServer.FreeTcpPort();

            _server ??= PreviewServer.CreateHostBuilder(UserSettingsSingleton.CurrentSettings().SiteDomainName,
                UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName, serverPort).Build();

            window.StatusContext.RunFireAndForgetWithToastOnError(async () =>
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                await _server.RunAsync();
            });
        }

        window.PreviewContext = new SitePreviewContext(UserSettingsSingleton.CurrentSettings().SiteDomainName,
            UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
            UserSettingsSingleton.CurrentSettings().SiteName, $"localhost:{serverPort}", window.StatusContext,
            initialUrl);

        return window;
    }
}