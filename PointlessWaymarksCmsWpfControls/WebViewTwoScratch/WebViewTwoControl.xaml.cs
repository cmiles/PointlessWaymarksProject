using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.WebViewTwoScratch
{
    /// <summary>
    ///     Interaction logic for WebViewTwoControl.xaml
    /// </summary>
    public partial class WebViewTwoControl : INotifyPropertyChanged
    {
        private string _cachedHtml;
        private WebViewTwoContext _context;

        public WebViewTwoControl()
        {
            InitializeComponent();
        }

        public string CachedHtml
        {
            get => _cachedHtml;
            set
            {
                if (value == _cachedHtml) return;
                _cachedHtml = value;
                OnPropertyChanged();
            }
        }

        public WebViewTwoContext Context
        {
            get => _context;
            set
            {
                if (Equals(value, _context)) return;
                _context = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async void ContextOnThresholdReached(object? sender, WebViewTwoContext.HtmlUpdatedEventArgs e)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            if (ControlWebView.IsInitialized && ControlWebView?.CoreWebView2 != null)
                ControlWebView.CoreWebView2.NavigateToString(e.HtmlString);
            else
                CachedHtml = e.HtmlString;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void WebView2_OnCoreWebView2Ready(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CachedHtml))
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                ControlWebView.NavigateToString(CachedHtml);
            }
        }

        private void WebViewTwoControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is WebViewTwoContext context)
            {
                Context = context;
                Context.ThresholdReached += ContextOnThresholdReached;
            }
            else
            {
                Context = null;
            }
        }

        private async void WebViewTwoControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            await ControlWebView.EnsureCoreWebView2Async();
        }
    }
}