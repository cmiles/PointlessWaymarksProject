using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PointlessWaymarks.LocalViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var optionsResult = Parser.Default.ParseArguments<CommandLineOptions>(e.Args);

            var options = (optionsResult as Parsed<CommandLineOptions>)?.Value;

            var freePort = FreeTcpPort();

            var server = CreateHostBuilder(new string[] {
                options.Url, options.Folder
            }, freePort).Build();

            Task.Run(() => server.RunAsync());

            new MainWindow(options.Url, options.Folder, options.SiteName).Show();
        }

        public static IHostBuilder CreateHostBuilder(string[] args, int port) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(options => options.Listen(IPAddress.Loopback, port))
                        .UseStartup<Startup>()
                        .UseSetting("ListeningPort", port.ToString())
                        .UseSetting("PreviewUrl", args[0])
                        .UseSetting("FileSystemRoot", new DirectoryInfo(args[1]).FullName);
                    ;
                });

        static int FreeTcpPort()
        {
            //https://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

    public class Startup
    {

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, ILogger<Startup> logger, IConfiguration configuration)
        {
            var listeningPort = configuration.GetValue<int>("ListeningPort");
            var fileRoot = configuration.GetValue<string>("FileSystemRoot");
            var previewUrl = configuration.GetValue<string>("PreviewUrl");

            app.UseDeveloperExceptionPage();

            app.Use(async (context, next) =>
            {
                var possiblePath = context.Request.Path;
                string moddedFile;

                if (possiblePath == null || string.IsNullOrWhiteSpace(possiblePath.Value) ||
                    possiblePath.Value == "/")
                {
                    moddedFile =
                        (await File.ReadAllTextAsync(Path.Join(fileRoot, "index.html"))).Replace($"https://{previewUrl}",
                            $"http://localhost:{listeningPort}", StringComparison.InvariantCultureIgnoreCase).Replace($"//{previewUrl}", $"//localhost:{listeningPort}", StringComparison.InvariantCultureIgnoreCase);
                    await context.Response.WriteAsync(moddedFile);
                }
                else if (possiblePath.Value.EndsWith(".html"))
                {
                    {
                        moddedFile =
                            (await File.ReadAllTextAsync(Path.Join(fileRoot, possiblePath))).Replace(
                                $"https://{previewUrl}", $"http://localhost:{listeningPort}", StringComparison.InvariantCultureIgnoreCase).Replace($"//{previewUrl}", $"//localhost:{listeningPort}", StringComparison.InvariantCultureIgnoreCase);
                        await context.Response.WriteAsync(moddedFile);
                    }
                }
                else
                {
                    await next.Invoke();
                }
            });

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(fileRoot),
                RequestPath = "",
                EnableDirectoryBrowsing = true
            });
        }
    }
}

