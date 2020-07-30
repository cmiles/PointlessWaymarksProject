using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using Windows.Media.SpeechSynthesis;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.BodyContentEditor
{
    public partial class BodyContentEditorControl : UserControl
    {
        public BodyContentEditorControl()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var text = SelectedText();

            if (string.IsNullOrWhiteSpace(text)) return;

            using var synthesizer = new SpeechSynthesizer();
            using var synthesizerStream = await synthesizer.SynthesizeTextToStreamAsync(text);
            await using var stream = synthesizerStream.AsStreamForRead();
            using var player = new SoundPlayer {Stream = stream};
            player.Play();
        }

        private string SelectedText()
        {
            try
            {
                return BodyContentWebView.InvokeScript("eval", "document.getSelection().toString();");
            }
            catch
            {
                return string.Empty;
            }
        }

        private void WebView_OnNavigationStarting(object sender, WebViewControlNavigationStartingEventArgs e)
        {
            if (e.Uri != null && e.Uri.AbsoluteUri == "about:blank")
            {
                e.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(e.Uri?.OriginalString)) return;

            e.Cancel = true;
            ProcessHelpers.OpenUrlInExternalBrowser(e.Uri?.OriginalString);
        }
    }
}