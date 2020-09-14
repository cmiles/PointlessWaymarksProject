using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using Windows.Media.SpeechSynthesis;

namespace PointlessWaymarksCmsWpfControls.BodyContentEditor
{
    /// <summary>
    ///     Interaction logic for BodyContentEditorHorizontalControl.xaml
    /// </summary>
    public partial class BodyContentEditorHorizontalControl : UserControl
    {
        public BodyContentEditorHorizontalControl()
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
                return BodyContentWebView.ExecuteScriptAsync("document.getSelection().toString();").Result;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}