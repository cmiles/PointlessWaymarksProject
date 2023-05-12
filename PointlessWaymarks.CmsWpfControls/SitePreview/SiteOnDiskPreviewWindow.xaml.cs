using Microsoft.Extensions.Hosting;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

/// <summary>
///     Interaction logic for SiteOnDiskPreviewWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033
public partial class SiteOnDiskPreviewWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private SitePreviewContext? _previewContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    private SiteOnDiskPreviewWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed. Does not show the window - consider using
    /// PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
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