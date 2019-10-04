using System.Windows;
using System.Windows.Controls;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.WpfHtml
{
    public static class BrowserHtmlBindingBehavior
    {
        //<WebBrowser lcl:BrowserBehavior.Html="{Binding HtmlToDisplay}" />

        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserHtmlBindingBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebBrowser wb)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                wb.NavigateToString(e.NewValue as string ?? "<h2>Loading...</h2>".ToHtmlDocument("...", string.Empty));
            }
        }
    }
}