using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarksCmsWpfControls.WpfHtml
{
    public class WebViewHtmlStringBindingBehavior : Behavior<WebView2>
    {
        private List<FileInfo> _previousFiles = new();

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
                    try
                    {
                        var newString = e.NewValue as string ?? "<h2>...</h2>".ToHtmlDocument("...", string.Empty);

                        if (!string.IsNullOrWhiteSpace(newString))
                        {
                            var newFile = new FileInfo(Path.Combine(
                                UserSettingsUtilities.TempStorageHtmlDirectory().FullName,
                                $"TempHtml-{Guid.NewGuid()}.html"));
                            await File.WriteAllTextAsync(newFile.FullName, newString);
                            bindingBehavior.AssociatedObject.CoreWebView2.Navigate($"file:////{newFile.FullName}");

                            if (!bindingBehavior._previousFiles.Any()) return;

                            foreach (var loopFiles in bindingBehavior._previousFiles)
                                try
                                {
                                    loopFiles.Delete();
                                }
                                catch (Exception exception)
                                {
                                    Console.WriteLine(exception);
                                    throw;
                                }

                            bindingBehavior._previousFiles.ForEach(x => x.Refresh());
                            bindingBehavior._previousFiles.RemoveAll(x => !x.Exists);
                        }
                        else
                        {
                            bindingBehavior.AssociatedObject.NavigateToString(e.NewValue as string ??
                                                                              "<h2>Loading...</h2>".ToHtmlDocument(
                                                                                  "...", string.Empty));
                        }
                    }
                    catch (Exception ex)
                    {
                        EventLogContext.TryWriteExceptionToLogBlocking(ex, "WebViewHtmlStringBindingBehavior",
                            string.Empty);
                    }
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

        private async void OnReady(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CachedHtml))
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                AssociatedObject.NavigateToString(CachedHtml);
            }
        }
    }
}