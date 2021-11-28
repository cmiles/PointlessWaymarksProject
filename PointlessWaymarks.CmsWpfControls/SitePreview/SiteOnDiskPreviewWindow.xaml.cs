using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

/// <summary>
///     Interaction logic for SiteOnDiskPreviewWindow.xaml
/// </summary>
[ObservableObject]
public partial class SiteOnDiskPreviewWindow
{
    [ObservableProperty] private SitePreviewContext _previewContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    public SiteOnDiskPreviewWindow()
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        DataContext = this;

        var freePort = PreviewServer.FreeTcpPort();

        var server = PreviewServer.CreateHostBuilder(UserSettingsSingleton.CurrentSettings().SiteUrl,
            UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName, freePort).Build();

        StatusContext.RunFireAndForgetWithToastOnError(async () =>
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            await server.RunAsync();
        });

        PreviewContext = new SitePreviewContext(UserSettingsSingleton.CurrentSettings().SiteUrl,
            UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName,
            UserSettingsSingleton.CurrentSettings().SiteName, $"localhost:{freePort}", StatusContext);
    }
}