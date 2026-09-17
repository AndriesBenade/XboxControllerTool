using XboxControllerTool.Simulation;

namespace XboxControllerTool.Tests;

public sealed class RecordingKeyboard : IKeyboardInput
{
    public List<string> Actions { get; } = [];

    public List<(KeyModifiers Modifiers, ushort VirtualKey)> Combinations { get; } = [];

    public void StartVoiceTyping() => Actions.Add(nameof(StartVoiceTyping));
    public void StopVoiceTyping() => Actions.Add(nameof(StopVoiceTyping));
    public void SendShowDesktop() => Actions.Add(nameof(SendShowDesktop));
    public void SendEscape() => Actions.Add(nameof(SendEscape));
    public void SendAltLeft() => Actions.Add(nameof(SendAltLeft));
    public void SendAltRight() => Actions.Add(nameof(SendAltRight));
    public void SendEnter() => Actions.Add(nameof(SendEnter));
    public void ArrowLeftDown() => Actions.Add(nameof(ArrowLeftDown));
    public void ArrowLeftUp() => Actions.Add(nameof(ArrowLeftUp));
    public void ArrowRightDown() => Actions.Add(nameof(ArrowRightDown));
    public void ArrowRightUp() => Actions.Add(nameof(ArrowRightUp));
    public void BackspaceDown() => Actions.Add(nameof(BackspaceDown));
    public void BackspaceUp() => Actions.Add(nameof(BackspaceUp));
    public void ReleaseModifiers() => Actions.Add(nameof(ReleaseModifiers));

    public void SendCombination(KeyModifiers modifiers, ushort virtualKey)
    {
        Actions.Add(nameof(SendCombination));
        Combinations.Add((modifiers, virtualKey));
    }
}
