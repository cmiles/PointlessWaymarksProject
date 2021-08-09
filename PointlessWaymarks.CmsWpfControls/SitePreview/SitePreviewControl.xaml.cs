using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.SitePreview
{
    /// <summary>
    ///     Interaction logic for SitePreviewControl.xaml
    /// </summary>
    public partial class SitePreviewControl : INotifyPropertyChanged
    {
        private SitePreviewContext _previewContext;

        public SitePreviewControl()
        {
            InitializeComponent();

            DataContext = PreviewContext;
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

        public event PropertyChangedEventHandler PropertyChanged;

        private async void InitializeAsync()
        {
            // must create a data folder if running out of a secured folder that can't write like Program Files
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(Path.GetTempPath(), "PointWaymarksCms_SitePreviewBrowserData"));

            // Note this waits until the first page is navigated!
            await SitePreviewWebView.EnsureCoreWebView2Async(env);

            SitePreviewWebView.Source = new Uri(PreviewContext.InitialPage);
        }

        public void LoadData()
        {
            InitializeAsync();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void SitePreviewControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SitePreviewContext context)
            {
                PreviewContext = context;
                context.WebViewGui = SitePreviewWebView;
                LoadData();
            }
        }

        private void SitePreviewWebView_OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Uri))
            {
                e.Cancel = true;
                PreviewContext.StatusContext.ToastError("Blank URL for navigation?");
                return;
            }

            if (!e.Uri.ToLower().StartsWith("http"))
            {
                e.Cancel = true;
                PreviewContext.StatusContext.ToastError(
                    "This window only supports http and https (no ftp, no searches, ...");
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
                PreviewContext.StatusContext.ToastError($"Trouble parsing {e.Uri}? {exception.Message}");
                return;
            }

            if (parsedUri.Authority.ToLower() != PreviewContext.PreviewServerHost.ToLower())
            {
                e.Cancel = true;
                ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
                PreviewContext.StatusContext.ToastError($"Sending external link {e.Uri} to the default browser.");
                PreviewContext.TextBarAddress = new Uri(PreviewContext.CurrentAddress).PathAndQuery;
                return;
            }

            PreviewContext.TextBarAddress = new Uri(e.Uri).PathAndQuery;
        }
    }
}