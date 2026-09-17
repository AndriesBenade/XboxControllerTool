using XboxControllerTool.Configuration;

namespace XboxControllerTool.Audio;

public sealed class AudioFeedbackPlayer : IAudioFeedbackPlayer
{
    private readonly AppSettings _settings;

    public AudioFeedbackPlayer(AppSettings settings)
    {
        _settings = settings;
    }

    public void PlayVoiceInputStarted() => PlayTone(880, 110);

    public void PlayVoiceInputStopped() => PlayTone(440, 110);

    private void PlayTone(int frequencyHz, int durationMs)
    {
        if (!_settings.AudioFeedbackEnabled)
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                Console.Beep(frequencyHz, durationMs);
            }
            catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException)
            {
            }
        });
    }
}
