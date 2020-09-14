using System;
using System.Windows;
using System.Windows.Controls;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
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
            await ThreadSwitcher.ResumeForegroundAsync();

            var text = await BodyContentWebView.ExecuteScriptAsync("document.getSelection().toString();");

            if (string.IsNullOrWhiteSpace(text)) return;

            //See https://github.com/jamesmontemagno/TextToSpeechPlugin/blob/master/src/TextToSpeech.Plugin/TextToSpeech.uwp.cs for
            //some details on a more robust setup

            using var synthesizer = new SpeechSynthesizer();
            using var synthesizerStream = await synthesizer.SynthesizeTextToStreamAsync(text);
            var player = BackgroundMediaPlayer.Current;
            player.AutoPlay = false;
            player.SetStreamSource(synthesizerStream);
            player.Play();
        }

        private string SelectedText()
        {
            try
            {
                //TODO: Revisit this async method
                return BodyContentWebView.ExecuteScriptAsync("document.getSelection().toString();").Result;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}