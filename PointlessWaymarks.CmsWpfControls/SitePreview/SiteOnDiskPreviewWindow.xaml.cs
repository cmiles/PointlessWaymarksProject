using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.SitePreview
{
    /// <summary>
    ///     Interaction logic for SiteOnDiskPreviewWindow.xaml
    /// </summary>
    public partial class SiteOnDiskPreviewWindow : INotifyPropertyChanged
    {
        private string _currentAddress;
        private string _initialPage;
        private string _localSiteUrl;
        private string _sourceUrl;
        private string _textBarAddress;
        private string _localSiteFolder;
        private string _windowTitle;

        public SiteOnDiskPreviewWindow()
        {
            InitializeComponent();

            StatusContext = new StatusControlContext();

            LocalSiteUrl = UserSettingsSingleton.CurrentSettings().SiteUrl;
            LocalSiteFolder = UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory;
            WindowTitle =
                $"{UserSettingsSingleton.CurrentSettings().SiteName} Preview - {LocalSiteFolder} is mapped to {LocalSiteUrl}";
            

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

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (value == _windowTitle) return;
                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public string LocalSiteFolder
        {
            get => _localSiteFolder;
            set
            {
                if (value == _localSiteFolder) return;
                _localSiteFolder = value;
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

        public string LocalSiteUrl
        {
            get => _localSiteUrl;
            set
            {
                if (value == _localSiteUrl) return;
                _localSiteUrl = value;
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

        public StatusControlContext StatusContext { get; set; }

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

        public Command TryGoBackNavigationCommand { get; set; }

        public Command TryGoForwardNavigationCommand { get; set; }

        public Command TryNavigateHomeCommand { get; set; }

        public Command TryRefreshCommand { get; set; }

        public Command TryUserNavigationCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void InitializeAsync()
        {
            // must create a data folder if running out of a secured folder that can't write like Program Files
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(Path.GetTempPath(), "PointWaymarksCms_SitePreviewBrowserData"));

            // Note this waits until the first page is navigated!
            await SitePreviewWebView.EnsureCoreWebView2Async(env);

            // Optional: Map a folder from the Executable Folder to a virtual domain
            // NOTE: This requires a Canary preview currently (.720+)
            SitePreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                UserSettingsSingleton.CurrentSettings().SiteUrl,
                UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory,
                CoreWebView2HostResourceAccessKind.Allow);
        }

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
                StatusContext.ToastError("Blank URL for navigation?");
                return;
            }

            if (!e.Uri.ToLower().StartsWith("http"))
            {
                e.Cancel = true;
                StatusContext.ToastError("This window only supports http and https (no ftp, no searches, ...");
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
                StatusContext.ToastError($"Trouble parsing {e.Uri}? {exception.Message}");
                return;
            }

            if (parsedUri.Host.ToLower() != LocalSiteUrl.ToLower())
            {
                e.Cancel = true;
                ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
                StatusContext.ToastError($"Sending external link {e.Uri} to the default browser.");
                TextBarAddress = CurrentAddress;
                return;
            }

            TextBarAddress = e.Uri;
        }

        private async Task TryGoBackNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            if (SitePreviewWebView.CoreWebView2.CanGoBack) SitePreviewWebView.CoreWebView2.GoBack();
        }

        private async Task TryGoForwardNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            if (SitePreviewWebView.CoreWebView2.CanGoForward) SitePreviewWebView.CoreWebView2.GoForward();
        }

        private async Task TryNavigateHome()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SitePreviewWebView.CoreWebView2.Navigate(InitialPage);
        }

        private async Task TryRefresh()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SitePreviewWebView.CoreWebView2.Reload();
        }

        private async Task TryUserNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SitePreviewWebView.CoreWebView2.Navigate(TextBarAddress);
        }
    }
}