using System.Text.Json;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.CmsWpfControls.Server;

public static class PartialContentPreviewServer
{
    private static PreviewServer? _previewServer;

    public static string PreviewServerLoadPreviewPageUrl =>
        $"http://localhost:{_previewServer.ServerPort}/localapi/loadpreviewpage";

    public static string PreviewServerUrl => $"http://localhost:{_previewServer.ServerPort}";

    public static async Task<PreviewServer> PartialContentServer()
    {
        if (_previewServer == null)
        {
            _previewServer = new PreviewServer();
            await _previewServer.StartServer(UserSettingsSingleton.CurrentSettings().SiteDomainName,
                UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName);
        }

        return _previewServer;
    }

    public static string ServerLoadPreviewPage(Guid requesterId, string body)
    {
        var siteDomain = UserSettingsSingleton.CurrentSettings().SiteDomainName;

        var html = $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head data-generationversion="2024-02-14T11:02:44.0000000" lang="en" dir="ltr">
                        <meta charset="utf-8">
                        <title>Pointless Waymarks Preview</title>
                        <link rel="stylesheet" href="http://{siteDomain}/style.css?v=1.0">
                        <link rel="shortcut icon" href="http://{siteDomain}/favicon.ico"/>
                        <link rel="stylesheet" href="http://{siteDomain}/SiteResources/leaflet.css" />
                        <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
                        <script src="http://{siteDomain}/SiteResources/leaflet.js"></script>
                        <link rel="stylesheet" href="http://{siteDomain}/SiteResources/leaflet-gesture-handling.min.css" type="text/css">
                        <script src="http://{siteDomain}/SiteResources/leaflet-gesture-handling.min.js"></script>
                        <link rel="stylesheet" href="http://{siteDomain}/SiteResources/L.Control.Locate.min.css" type="text/css">
                        <script src="http://{siteDomain}/SiteResources/L.Control.Locate.min.js"></script>
                        <script src="http://{siteDomain}/SiteResources/pointless-waymarks-spatial-common.js"></script>
                    </head>

                    <body>
                        <div class="content-container" data-contentversion="2024-02-14T09:53:54.0000000">
                        {body}
                    </div>
                    </body>

                    </html>
                    """;

        return JsonSerializer.Serialize(new ServerLoadPreviewPage { RequesterId = requesterId, ToPreview = html });
    }
}