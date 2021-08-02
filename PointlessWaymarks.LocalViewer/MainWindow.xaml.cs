using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Diagnostics;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.LocalViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private StatusControlContext _statusContext;
        private SitePreviewContext _previewContext;

        public MainWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            DataContext = this;
        }

        public MainWindow(string siteUrl, string localFolder, string siteName)
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            DataContext = this;

            if (string.IsNullOrWhiteSpace(localFolder)) localFolder = Environment.CurrentDirectory;

            if (string.IsNullOrWhiteSpace(siteUrl) || string.IsNullOrWhiteSpace(siteName))
            {
                var possibleFile = Directory.EnumerateDirectories(Environment.CurrentDirectory, "index.htm*")
                    .OrderBy(x => x.Length).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(possibleFile))
                {
                    var urlFound = !string.IsNullOrWhiteSpace(siteUrl);
                    var siteNameFound = !string.IsNullOrWhiteSpace(siteName);

                    foreach (var loopLine in File.ReadLines(possibleFile))
                    {
                        if (!urlFound)
                        {
                            var urlString = Regex.Match(loopLine, "<meta property=\"og:url\" content=\"(?<contentUrl>.*)\">", RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

                            if (!string.IsNullOrWhiteSpace(urlString))
                            {
                                urlFound = true;
                                siteUrl = new Uri(urlString).Host;
                            }
                        }

                        if (!siteNameFound)
                        {
                            var siteNameString = Regex.Match(loopLine, "<meta property=\"og:site_name\" content=\"(?<contentUrl>.*)\">", RegexOptions.IgnoreCase).Groups["contentUrl"].Value;

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

            PreviewContext = new SitePreviewContext(siteUrl,
                localFolder,
                siteName, StatusContext);
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
