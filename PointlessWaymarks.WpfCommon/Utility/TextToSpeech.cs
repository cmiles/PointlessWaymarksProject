using System.Diagnostics;
using System.Security;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace PointlessWaymarks.WpfCommon.Utility;

/// <summary>
///     Text To Speech Implementation Windows - modified from
///     https://github.com/jamesmontemagno/TextToSpeechPlugin/blob/master/src/TextToSpeech.Plugin/TextToSpeech.uwp.cs - MIT
///     License
/// </summary>
public class TextToSpeech : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SpeechSynthesizer? _speechSynthesizer;


    /// <summary>
    ///     SpeechSynthesizer
    /// </summary>
    public TextToSpeech()
    {
        _speechSynthesizer = new SpeechSynthesizer();
    }

    /// <summary>
    ///     Dispose of TTS
    /// </summary>
    public void Dispose()
    {
        _speechSynthesizer?.Dispose();
    }


    /// <summary>
    ///     Speak back text
    /// </summary>
    /// <param name="text">Text to speak</param>
    /// <param name="cancelToken">Cancellation token to stop speak</param>
    /// <exception cref="ArgumentNullException">Thrown if text is null</exception>
    /// <exception cref="ArgumentException">Thrown if text length is greater than maximum allowed</exception>
    public async Task Speak(string? text, CancellationToken cancelToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            await _semaphore.WaitAsync(cancelToken);

            if (_speechSynthesizer == null) return;

            _speechSynthesizer.Voice = SpeechSynthesizer.DefaultVoice;

            var localCode = SpeechSynthesizer.DefaultVoice.Language;

            var pitchProsody = "default";

            var ssMl = @"<speak version='1.0' " +
                       $"xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{localCode}'>" +
                       $"<prosody pitch='{pitchProsody}' rate='1.0'>{SecurityElement.Escape(text)}</prosody> " +
                       "</speak>";

            var tcs = new TaskCompletionSource<object>();
            var handler = new TypedEventHandler<MediaPlayer, object>((_, _) => tcs.TrySetResult(new object()));

            try
            {
                var player = BackgroundMediaPlayer.Current;
                var stream = await _speechSynthesizer.SynthesizeSsmlToStreamAsync(ssMl);

                player.MediaEnded += handler;
                player.SetStreamSource(stream);
                player.Play();

                void OnCancel()
                {
                    player.PlaybackRate = 0;
                    tcs.TrySetResult(new object());
                }

                await using (cancelToken.Register(OnCancel))
                {
                    await tcs.Task;
                }

                player.MediaEnded -= handler;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to playback stream: " + ex);
            }
        }
        finally
        {
            if (_semaphore.CurrentCount == 0)
                _semaphore.Release();
        }
    }
}