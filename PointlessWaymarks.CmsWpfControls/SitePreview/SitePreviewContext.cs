using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using DocumentFormat.OpenXml.Drawing;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.SitePreview
{
    public class SitePreviewContext : DependencyObject, INotifyPropertyChanged
    {
        private string _currentAddress;
        private string _initialPage;
        private string _localSiteFolder;
        private string _previewServerHost;
        private string _siteName;
        private string _siteUrl;
        private StatusControlContext _statusContext;
        private string _textBarAddress;

        private Command _tryGoBackNavigationCommand;
        private Command _tryGoForwardNavigationCommand;
        private Command _tryNavigateHomeCommand;
        private Command _tryRefreshCommand;
        private Command _tryUserNavigationCommand;
        private string _windowTitle;

        public SitePreviewContext(string siteUrl, string localSiteFolder, string siteName, string previewServerHost,
            StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            SiteUrl = siteUrl;
            LocalSiteFolder = localSiteFolder;
            SiteName = siteName;
            PreviewServerHost = previewServerHost;

            InitialPage = string.IsNullOrEmpty(InitialPage)
                ? $"http://{previewServerHost}/index.html"
                : InitialPage;

            WindowTitle = string.IsNullOrWhiteSpace(SiteName)
                ? $"Preview - {LocalSiteFolder} is mapped to {SiteUrl}"
                : $"{SiteName} - {LocalSiteFolder} is mapped to {SiteUrl}";

            CurrentAddress = InitialPage;

            TryUserNavigationCommand = StatusContext.RunBlockingTaskCommand(TryUserNavigation);
            TryGoBackNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoBackNavigation);
            TryGoForwardNavigationCommand = StatusContext.RunBlockingTaskCommand(TryGoForwardNavigation);
            TryRefreshCommand = StatusContext.RunBlockingTaskCommand(TryRefresh);
            TryNavigateHomeCommand = StatusContext.RunBlockingTaskCommand(TryNavigateHome);
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

        public string PreviewServerHost
        {
            get => _previewServerHost;
            set
            {
                if (value == _previewServerHost) return;
                _previewServerHost = value;
                OnPropertyChanged();
            }
        }

        public string SiteName
        {
            get => _siteName;
            set
            {
                if (value == _siteName) return;
                _siteName = value;
                OnPropertyChanged();
            }
        }

        public string SiteUrl
        {
            get => _siteUrl;
            set
            {
                if (value == _siteUrl) return;
                _siteUrl = value;
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

        public Command TryGoBackNavigationCommand
        {
            get => _tryGoBackNavigationCommand;
            set
            {
                if (Equals(value, _tryGoBackNavigationCommand)) return;
                _tryGoBackNavigationCommand = value;
                OnPropertyChanged();
            }
        }

        public Command TryGoForwardNavigationCommand
        {
            get => _tryGoForwardNavigationCommand;
            set
            {
                if (Equals(value, _tryGoForwardNavigationCommand)) return;
                _tryGoForwardNavigationCommand = value;
                OnPropertyChanged();
            }
        }

        public Command TryNavigateHomeCommand
        {
            get => _tryNavigateHomeCommand;
            set
            {
                if (Equals(value, _tryNavigateHomeCommand)) return;
                _tryNavigateHomeCommand = value;
                OnPropertyChanged();
            }
        }

        public Command TryRefreshCommand
        {
            get => _tryRefreshCommand;
            set
            {
                if (Equals(value, _tryRefreshCommand)) return;
                _tryRefreshCommand = value;
                OnPropertyChanged();
            }
        }

        public Command TryUserNavigationCommand
        {
            get => _tryUserNavigationCommand;
            set
            {
                if (Equals(value, _tryUserNavigationCommand)) return;
                _tryUserNavigationCommand = value;
                OnPropertyChanged();
            }
        }

        public WebView2 WebViewGui { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task TryGoBackNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            if (WebViewGui.CoreWebView2.CanGoBack) WebViewGui.CoreWebView2.GoBack();
        }

        private async Task TryGoForwardNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            if (WebViewGui.CoreWebView2.CanGoForward) WebViewGui.CoreWebView2.GoForward();
        }

        private async Task TryNavigateHome()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            WebViewGui.CoreWebView2.Navigate(InitialPage);
        }

        private async Task TryRefresh()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            WebViewGui.CoreWebView2.Reload();
        }

        private async Task TryUserNavigation()
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            WebViewGui.CoreWebView2.Navigate($"http://{StringHelpers.UrlCombine(SiteUrl, TextBarAddress)}");
        }
    }
}