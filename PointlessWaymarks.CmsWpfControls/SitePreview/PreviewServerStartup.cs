using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

public class PreviewServerStartup
{
    public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime,
        ILogger<PreviewServerStartup> logger, IConfiguration configuration)
    {
        var previewListeningPort = configuration.GetValue<int>("PreviewPort");
        var previewFileRoot = configuration.GetValue<string>("PreviewFileSystemRoot");
        var previewHost = configuration.GetValue<string>("PreviewHost");

        app.UseDeveloperExceptionPage();

        app.Use(async (context, next) =>
        {
            var possiblePath = context.Request.Path;

            if (possiblePath == null || string.IsNullOrWhiteSpace(possiblePath.Value) ||
                possiblePath.Value == "/")
            {
                var moddedFile = (await File.ReadAllTextAsync(Path.Join(previewFileRoot, "index.html"))).Replace(
                        $"https://{previewHost}",
                        $"http://{previewHost}", StringComparison.OrdinalIgnoreCase)
                    .Replace($"//{previewHost}", $"//localhost:{previewListeningPort}",
                        StringComparison.OrdinalIgnoreCase);
                await context.Response.WriteAsync(moddedFile);
            }
            else if (possiblePath.Value.EndsWith(".html"))
            {
                var rawFile = new StringBuilder();

                using (var sr = File.OpenText(Path.Join(previewFileRoot, possiblePath)))
                {
                    while (await sr.ReadLineAsync() is { } streamLine)
                        rawFile.AppendLine(streamLine.Replace(
                            $"https://{previewHost}", $"http://{previewHost}",
                            StringComparison.OrdinalIgnoreCase).Replace($"//{previewHost}",
                            $"//localhost:{previewListeningPort}", StringComparison.OrdinalIgnoreCase));
                }

                await context.Response.WriteAsync(rawFile.ToString());
            }
            else
            {
                await next.Invoke();
            }
        });

        app.UseFileServer(new FileServerOptions
        {
            FileProvider = new PhysicalFileProvider(previewFileRoot),
            RequestPath = "",
            EnableDirectoryBrowsing = true
        });
    }
}