using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace PointlessWaymarks.CmsWpfControls.Server;

public class PreviewServer
{
    private Dictionary<Guid, string> _previewPages = new();
    public int ServerPort { get; } = FreeTcpPort();
    
    public static int FreeTcpPort()
    {
        //https://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
    
    public async Task StartServer(string siteDomainName, string previewFileRootDirectory)
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.WebHost.ConfigureKestrel(x => x.ListenLocalhost(ServerPort));
        
        var app = builder.Build();
        
        app.UseDeveloperExceptionPage();
        
        app.Use(async (context, next) =>
        {
            var possiblePath = context.Request.Path;
            
            if (string.IsNullOrWhiteSpace(possiblePath.Value) ||
                possiblePath.Value == "/")
            {
                var moddedFile = (await File.ReadAllTextAsync(Path.Join(previewFileRootDirectory, "index.html")))
                    .Replace(
                        $"https://{siteDomainName}",
                        $"http://{siteDomainName}", StringComparison.OrdinalIgnoreCase)
                    .Replace($"//{siteDomainName}", $"//localhost:{ServerPort}",
                        StringComparison.OrdinalIgnoreCase);
                await context.Response.WriteAsync(moddedFile);
            }
            else if (possiblePath.Value.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                     possiblePath.Value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var rawFile = new StringBuilder();
                
                using (var sr = File.OpenText(Path.Join(previewFileRootDirectory, possiblePath)))
                {
                    while (await sr.ReadLineAsync() is { } streamLine)
                        rawFile.AppendLine(streamLine.Replace(
                            $"https://{siteDomainName}", $"http://{siteDomainName}",
                            StringComparison.OrdinalIgnoreCase).Replace($"//{siteDomainName}",
                            $"//localhost:{ServerPort}", StringComparison.OrdinalIgnoreCase));
                }
                
                await context.Response.WriteAsync(rawFile.ToString());
            }
            else
            {
                await next.Invoke();
            }
        });
        
        var provider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                // Add new mappings
                [".flac"] = "audio/flac",
                [".gpx"] = "application/gpx+xml"
            }
        };
        
        app.UseFileServer(new FileServerOptions
        {
            FileProvider = new PhysicalFileProvider(previewFileRootDirectory),
            RequestPath = "",
            EnableDirectoryBrowsing = true,
            StaticFileOptions = { ContentTypeProvider = provider }
        });
        
        app.MapPost("/localapi/loadpreviewpage", (ServerLoadPreviewPage data) =>
        {
            _previewPages[data.RequesterId] = data.ToPreview;
            
            // Redirect to another action
            return Task.FromResult(Results.Redirect($"/localapi/showpreviewpage/{data.RequesterId}"));
        });
        
        app.MapGet("/localapi/showpreviewpage/{requester}", (Guid requester) =>
        {
            if (_previewPages.TryGetValue(requester, out var page))
            {
                var cleanedHtml = page.Replace(
                    $"https://{siteDomainName}", $"http://{siteDomainName}",
                    StringComparison.OrdinalIgnoreCase).Replace($"//{siteDomainName}",
                    $"//localhost:{ServerPort}", StringComparison.OrdinalIgnoreCase);
                return Results.Content(cleanedHtml, "text/html");
            }
            
            return Results.NotFound();
        });
        
        await app.RunAsync();
    }
}