using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.LocalViewer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private SitePreviewContext _previewContext;
        private StatusControlContext _statusContext;

        public MainWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext { BlockUi = false };

            DataContext = this;
        }

        public MainWindow(string siteUrl, string localFolder, string siteName)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext { BlockUi = false };

            DataContext = this;

            if (string.IsNullOrWhiteSpace(localFolder)) localFolder = Environment.CurrentDirectory;

            if (string.IsNullOrWhiteSpace(siteUrl) || string.IsNullOrWhiteSpace(siteName))
            {
                var possibleFile = Directory.EnumerateFiles(localFolder, "index.htm*")
                    .OrderBy(x => x.Length).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(possibleFile))
                {
                    var urlFound = !string.IsNullOrWhiteSpace(siteUrl);
                    var siteNameFound = !string.IsNullOrWhiteSpace(siteName);

                    foreach (var loopLine in File.ReadLines(possibleFile))
                    {
                        if (!urlFound)
                        {
                            var urlString = Regex
                                .Match(loopLine, "<meta property=\"og:url\" content=\"(?<contentUrl>.*)\">",
                                    RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

                            if (!string.IsNullOrWhiteSpace(urlString))
                            {
                                urlFound = true;
                                siteUrl = new Uri(urlString).Host;
                            }
                        }

                        if (!siteNameFound)
                        {
                            var siteNameString = Regex.Match(loopLine,
                                "<meta property=\"og:site_name\" content=\"(?<contentUrl>.*)\">",
                                RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

                            if (!string.IsNullOrWhiteSpace(siteNameString))
                            {
                                siteNameFound = true;
                                siteName = siteNameString;
                            }
                        }

                        if (urlFound && siteNameFound) break;

                        if (loopLine.Contains("</head>", StringComparison.InvariantCultureIgnoreCase)) break;
                    }
                }
            }

            var freePort = PreviewServer.FreeTcpPort();

            var server = PreviewServer.CreateHostBuilder(
                siteUrl, localFolder, freePort).Build();

            StatusContext.RunFireAndForgetWithToastOnError(async () =>
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                await server.RunAsync();
            });

            PreviewContext = new SitePreviewContext(siteUrl,
                localFolder,
                siteName, $"localhost:{freePort}", StatusContext);
        }

        public SitePreviewContext PreviewContext
        {
            get => _previewContext;
            set
            {
                if (Equals(value, _previewContext)) return;
                _previewContext = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}