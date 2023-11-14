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
    public static async Task<SiteOnDiskPreviewWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new SiteOnDiskPreviewWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var freePort = PreviewServer.FreeTcpPort();

        var server = PreviewServer.CreateHostBuilder(UserSettingsSingleton.CurrentSettings().SiteDomainName,
            UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName, freePort).Build();

        window.StatusContext.RunFireAndForgetWithToastOnError(async () =>
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            await server.RunAsync();
        });

        window.PreviewContext = new SitePreviewContext(UserSettingsSingleton.CurrentSettings().SiteDomainName,
            UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
            UserSettingsSingleton.CurrentSettings().SiteName, $"localhost:{freePort}", window.StatusContext);

        return window;
    }
}