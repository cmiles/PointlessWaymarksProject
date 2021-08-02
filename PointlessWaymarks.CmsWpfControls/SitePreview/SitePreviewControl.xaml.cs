using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using DocumentFormat.OpenXml.Drawing.Charts;
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
    /// Interaction logic for SitePreviewControl.xaml
    /// </summary>
    public partial class SitePreviewControl : INotifyPropertyChanged
    {



        public SitePreviewControl()
        {
            InitializeComponent();

            DataContext = this;
        }

        public void LoadData()
        {
            InitializeAsync();
        }

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
                PreviewContext.SiteUrl,
                PreviewContext.LocalSiteFolder,
                CoreWebView2HostResourceAccessKind.Allow);
            
            SitePreviewWebView.Source = new Uri(PreviewContext.InitialPage);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                PreviewContext.StatusContext.ToastError("This window only supports http and https (no ftp, no searches, ...");
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

            if (parsedUri.Host.ToLower() != PreviewContext.SiteUrl.ToLower())
            {
                e.Cancel = true;
                ProcessHelpers.OpenUrlInExternalBrowser(e.Uri);
                PreviewContext.StatusContext.ToastError($"Sending external link {e.Uri} to the default browser.");
                PreviewContext.TextBarAddress = PreviewContext.CurrentAddress;
                return;
            }

            PreviewContext.TextBarAddress = e.Uri;
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

        public SitePreviewContext PreviewContext { get; set; }
    }
}