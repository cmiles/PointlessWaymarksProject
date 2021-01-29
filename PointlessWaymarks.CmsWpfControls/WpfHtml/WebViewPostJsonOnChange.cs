using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.WpfHtml
{
    public class WebViewPostJsonOnChange : Behavior<WebView2>
    {
        public static readonly DependencyProperty JsonDataProperty = DependencyProperty.Register("JsonData",
            typeof(string), typeof(WebViewPostJsonOnChange), new PropertyMetadata(default(string), OnJsonDataChanged));

        public string CachedData { get; set; } = string.Empty;

        public string JsonData
        {
            get => (string) GetValue(JsonDataProperty);
            set => SetValue(JsonDataProperty, value);
        }

        private async void CoreWebView2OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CachedData))
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                await PostNewJson(this, CachedData);
            }
        }

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.CoreWebView2InitializationCompleted += OnReady;
        }

        private static async void OnJsonDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebViewPostJsonOnChange bindingBehavior) await PostNewJson(bindingBehavior, e.NewValue as string);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await AssociatedObject.EnsureCoreWebView2Async();
        }

        private void OnReady(object sender, EventArgs e)
        {
            AssociatedObject.CoreWebView2.WebMessageReceived += CoreWebView2OnWebMessageReceived;
        }

        private static async Task PostNewJson(WebViewPostJsonOnChange bindingBehavior, string toPost)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            if (bindingBehavior.AssociatedObject.IsInitialized && bindingBehavior.AssociatedObject.CoreWebView2 != null)
            {
                bindingBehavior.CachedData = string.Empty;

                try
                {
                    bindingBehavior.AssociatedObject.CoreWebView2.PostWebMessageAsJson(toPost);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "PostNewJson Error");
                }
            }
            else
            {
                bindingBehavior.CachedData = toPost;
            }
        }
    }
}