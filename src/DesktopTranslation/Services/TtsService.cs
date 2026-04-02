using System.Speech.Synthesis;

namespace DesktopTranslation.Services;

public class TtsService : IDisposable
{
    private readonly SpeechSynthesizer _synth = new();
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;

    public TtsService()
    {
        _synth.SpeakCompleted += (_, _) => _isSpeaking = false;
    }

    public void SetSpeed(double speed)
    {
        // Rate ranges from -10 to 10, default 0
        _synth.Rate = (int)Math.Clamp((speed - 1.0) * 10, -10, 10);
    }

    public void Speak(string text, string language)
    {
        Stop();
        SelectVoiceForLanguage(language);
        _isSpeaking = true;
        _synth.SpeakAsync(text);
    }

    public void Stop()
    {
        if (_isSpeaking)
        {
            _synth.SpeakAsyncCancelAll();
            _isSpeaking = false;
        }
    }

    private void SelectVoiceForLanguage(string language)
    {
        try
        {
            var culture = language.StartsWith("zh") ? "zh-TW" : "en-US";
            _synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult,
                0, new System.Globalization.CultureInfo(culture));
        }
        catch
        {
            // Fallback to default voice
        }
    }

    public void Dispose()
    {
        _synth.Dispose();
    }
}
