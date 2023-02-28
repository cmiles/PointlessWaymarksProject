using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

public static class PreviewServer
{
    public static IHostBuilder CreateHostBuilder(string previewHost, string fileSystemPreviewRoot, int port)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseKestrel(options => options.Listen(IPAddress.Loopback, port))
                    .UseSetting("PreviewPort", port.ToString())
                    .UseSetting("PreviewHost", previewHost)
                    .UseSetting("PreviewFileSystemRoot", new DirectoryInfo(fileSystemPreviewRoot).FullName)
                    .UseStartup<PreviewServerStartup>();
                ;
            });
    }

    public static int FreeTcpPort()
    {
        //https://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}