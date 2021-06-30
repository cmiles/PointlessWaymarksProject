using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml.Drawing;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Path = System.IO.Path;

namespace PointlessWaymarks.CmsWpfControls.SitePreview
{
    /// <summary>
    /// Interaction logic for SitePreviewWindow.xaml
    /// </summary>
    public partial class SitePreviewWindow : INotifyPropertyChanged
    {
        private string _url;
        private string _textBarAddress;
        private string _currentAddress;
        private string _sourceUrl;
        private string _previewHost;
        private string _initialPage;

        public SitePreviewWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            PreviewHost = UserSettingsSingleton.CurrentSettings().SiteUrl;

            InitialPage = $"https://{UserSettingsSingleton.CurrentSettings().SiteUrl}/index.html";
            SourceUrl = InitialPage;
            CurrentAddress = SourceUrl;

            DataContext = this;

            TryUserNavigationCommand = StatusContext.RunBlockingTaskCommand(TryUserNavigation);
            TryGoBackNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoBackNavigation);
            TryGoForwardNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoForwardNavigation);
            TryRefreshCommand = StatusContext.RunBlockingTaskCommand(TryRefresh);
            TryNavigateHomeCommand = StatusContext.RunBlockingTaskCommand(TryNavigateHome);

            InitializeAsync();
        }

        public string InitialPage
        {
            get => _initialPage;
            set
            {
                if (value == _initialPage) return;
                _initialPage = value;
                OnPropertyChanged();
            }
        }

        public Command TryNavigateHomeCommand { get; set; }

        public Command TryRefreshCommand { get; set; }

        public Command TryGoForwardNavigationCommand { get; set; }

        public Command TryGoBackNavigationCommand { get; set; }

        private async Task TryUserNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SitePreviewWebView.CoreWebView2.Navigate(TextBarAddress);
        }

        private async Task TryGoBackNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            if(SitePreviewWebView.CoreWebView2.CanGoBack) SitePreviewWebView.CoreWebView2.GoBack();
        }

        private async Task TryGoForwardNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            if (SitePreviewWebView.CoreWebView2.CanGoForward) SitePreviewWebView.CoreWebView2.GoForward();
        }

        private async Task TryRefresh()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SitePreviewWebView.CoreWebView2.Reload();
        }

        private async Task TryNavigateHome()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SitePreviewWebView.CoreWebView2.Navigate(InitialPage);
        }

        public Command TryUserNavigationCommand { get; set; }

        public StatusControlContext StatusContext { get; set; }

        public string PreviewHost
        {
            get => _previewHost;
            set
            {
                if (value == _previewHost) return;
                _previewHost = value;
                OnPropertyChanged();
            }
        }

        public string CurrentAddress
        {
            get => _currentAddress;
            set
            {
                if (value == _currentAddress) return;
                _currentAddress = value;
                OnPropertyChanged();
            }
        }

        public string TextBarAddress
        {
            get => _textBarAddress;
            set
            {
                if (value == _textBarAddress) return;
                _textBarAddress = value;
                OnPropertyChanged();
            }
        }

        public string SourceUrl
        {
            get => _sourceUrl;
            set
            {
                if (value == _sourceUrl) return;
                _sourceUrl = value;
                OnPropertyChanged();
            }
        }

        async void InitializeAsync()
        {
            // must create a data folder if running out of a secured folder that can't write like Program Files
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(Path.GetTempPath(), "PointWaymarksCms_SitePreviewBrowserData"));

            // Note this waits until the first page is navigated!
            await SitePreviewWebView.EnsureCoreWebView2Async(env);

            // Optional: Map a folder from the Executable Folder to a virtual domain
            // NOTE: This requires a Canary preview currently (.720+)
            SitePreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                UserSettingsSingleton.CurrentSettings().SiteUrl, UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory,
                CoreWebView2HostResourceAccessKind.Allow);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SitePreviewWebView_OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Uri))
            {
                e.Cancel = true;
                return;
            }

            if (!e.Uri.ToLower().StartsWith("http"))
            {
                e.Cancel = true;
                return;
            }

            Uri parsedUri;
            try
            {
                parsedUri = new Uri(e.Uri);
            }
            catch (Exception exception)
            {
                e.Cancel = true;
                return;
            }

            if (parsedUri.Host.ToLower() != PreviewHost.ToLower())
            {
                e.Cancel = true;
                ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
                TextBarAddress = CurrentAddress;
                return;
            }

            TextBarAddress = e.Uri;
        }
    }
}
