using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PointlessWaymarks.LocalViewer
{
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
                string moddedFile;

                if (possiblePath == null || string.IsNullOrWhiteSpace(possiblePath.Value) ||
                    possiblePath.Value == "/")
                {
                    moddedFile = File.ReadAllText(Path.Join(previewFileRoot, "index.html")).Replace(
                            $"https://{previewHost}",
                            $"http://localhost:{previewListeningPort}", StringComparison.InvariantCultureIgnoreCase)
                        .Replace($"//{previewHost}", $"//localhost:{previewListeningPort}",
                            StringComparison.InvariantCultureIgnoreCase);
                    await context.Response.WriteAsync(moddedFile);
                }
                else if (possiblePath.Value.EndsWith(".html"))
                {
                    var rawFile = new StringBuilder();

                    using (var sr = File.OpenText(Path.Join(previewFileRoot, possiblePath)))
                    {
                        string streamLine;
                        while ((streamLine = await sr.ReadLineAsync()) != null)
                            rawFile.AppendLine(streamLine.Replace(
                                $"https://{previewHost}", $"http://localhost:{previewListeningPort}",
                                StringComparison.InvariantCultureIgnoreCase).Replace($"//{previewHost}",
                                $"//localhost:{previewListeningPort}", StringComparison.InvariantCultureIgnoreCase));
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
}