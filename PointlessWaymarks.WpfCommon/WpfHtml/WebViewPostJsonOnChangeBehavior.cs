﻿using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using Serilog;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public class WebViewPostJsonOnChangeBehavior : Behavior<WebView2>
{
    public static readonly DependencyProperty JsonDataProperty = DependencyProperty.Register(nameof(JsonData),
        typeof(string), typeof(WebViewPostJsonOnChangeBehavior),
        new PropertyMetadata(default(string), OnJsonDataChanged));

    public string CachedJson { get; set; } = string.Empty;

    public string JsonData
    {
        get => (string)GetValue(JsonDataProperty);
        set => SetValue(JsonDataProperty, value);
    }

    private async void CoreWebView2OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!(e.WebMessageAsJson?.Contains("script-finished") ?? false)) return;

        if (string.IsNullOrWhiteSpace(CachedJson)) return;

        await ThreadSwitcher.ResumeForegroundAsync();
        await PostNewJson(this, CachedJson);
    }

    protected override void OnAttached()
    {
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.CoreWebView2InitializationCompleted += OnReady;
    }

    private static async void OnJsonDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebViewPostJsonOnChangeBehavior bindingBehavior)
            await PostNewJson(bindingBehavior, e.NewValue as string ?? string.Empty);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        await AssociatedObject.EnsureCoreWebView2Async();
    }

    private void OnReady(object? sender, EventArgs e)
    {
        AssociatedObject.CoreWebView2.WebMessageReceived += CoreWebView2OnWebMessageReceived;
    }

    private static async Task PostNewJson(WebViewPostJsonOnChangeBehavior bindingBehavior, string toPost)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (bindingBehavior.AssociatedObject.IsInitialized && bindingBehavior.AssociatedObject.CoreWebView2 != null)
        {
            bindingBehavior.CachedJson = string.Empty;

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
            bindingBehavior.CachedJson = toPost;
        }
    }
}