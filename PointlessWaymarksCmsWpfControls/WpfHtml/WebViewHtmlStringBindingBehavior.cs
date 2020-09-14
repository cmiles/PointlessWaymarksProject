using System;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.WpfHtml
{
    public class WebViewHtmlStringBindingBehavior : Behavior<WebView2>
    {
        public static readonly DependencyProperty HtmlStringProperty = DependencyProperty.Register("HtmlString",
            typeof(string), typeof(WebViewHtmlStringBindingBehavior),
            new PropertyMetadata(default(string), OnHtmlChanged));

        public string CachedHtml { get; set; }

        public string HtmlString
        {
            get => (string) GetValue(HtmlStringProperty);
            set => SetValue(HtmlStringProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.CoreWebView2Ready += OnReady;
        }

        private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebViewHtmlStringBindingBehavior bindingBehavior)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                if (bindingBehavior.AssociatedObject.IsInitialized &&
                    bindingBehavior.AssociatedObject.CoreWebView2 != null)
                {
                    bindingBehavior.CachedHtml = string.Empty;
                    bindingBehavior.AssociatedObject.NavigateToString(e.NewValue as string ??
                                                                      "<h2>Loading...</h2>".ToHtmlDocument("...",
                                                                          string.Empty));
                }
                else
                {
                    bindingBehavior.CachedHtml = e.NewValue as string ??
                                                 "<h2>Loading...</h2>".ToHtmlDocument("...", string.Empty);
                }
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await AssociatedObject.EnsureCoreWebView2Async();
        }

        private async void OnReady(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CachedHtml))
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                AssociatedObject.NavigateToString(CachedHtml);
            }
        }
    }
}