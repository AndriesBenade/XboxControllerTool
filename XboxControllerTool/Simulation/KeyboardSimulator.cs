using System.Runtime.InteropServices;

namespace XboxControllerTool.Simulation;

public sealed class KeyboardSimulator : IKeyboardInput
{
    public void StartVoiceTyping()
    {
        Send(KeyDown(NativeInput.VkLeftWindows), KeyDown(NativeInput.VkH), KeyUp(NativeInput.VkH), KeyUp(NativeInput.VkLeftWindows));
    }

    /// <summary>
    /// Sends only the Win+H toggle. An Escape used to be sent straight after to force the panel
    /// shut, but it went to whatever had focus rather than to the panel, so it could dismiss the
    /// user's own dialog and still leave the panel up. Closing it is handled by
    /// <see cref="Windows.IVoiceTypingFlyout"/>, which acts on the panel's own window.
    /// </summary>
    public void StopVoiceTyping()
    {
        Send(KeyDown(NativeInput.VkLeftWindows), KeyDown(NativeInput.VkH), KeyUp(NativeInput.VkH), KeyUp(NativeInput.VkLeftWindows));
    }

    public void SendShowDesktop()
    {
        Send(KeyDown(NativeInput.VkLeftWindows), KeyDown(NativeInput.VkD), KeyUp(NativeInput.VkD), KeyUp(NativeInput.VkLeftWindows));
    }

    public void SendEscape() => SendKeySequence(NativeInput.VkEscape);

    public void SendAltLeft()
    {
        Send(KeyDown(NativeInput.VkMenu), KeyDown(NativeInput.VkLeft), KeyUp(NativeInput.VkLeft), KeyUp(NativeInput.VkMenu));
    }

    public void SendAltRight()
    {
        Send(KeyDown(NativeInput.VkMenu), KeyDown(NativeInput.VkRight), KeyUp(NativeInput.VkRight), KeyUp(NativeInput.VkMenu));
    }

    public void SendEnter() => SendKeySequence(NativeInput.VkReturn);

    public void ArrowLeftDown() => Send(KeyDown(NativeInput.VkLeft));

    public void ArrowLeftUp() => Send(KeyUp(NativeInput.VkLeft));

    public void ArrowRightDown() => Send(KeyDown(NativeInput.VkRight));

    public void ArrowRightUp() => Send(KeyUp(NativeInput.VkRight));

    public void BackspaceDown() => Send(KeyDown(NativeInput.VkBack));

    public void BackspaceUp() => Send(KeyUp(NativeInput.VkBack));

    public void SendCombination(KeyModifiers modifiers, ushort virtualKey)
    {
        var sequence = new List<Input>(9);

        foreach (var modifier in ModifierKeys(modifiers))
        {
            sequence.Add(KeyDown(modifier));
        }

        sequence.Add(KeyDown(virtualKey));
        sequence.Add(KeyUp(virtualKey));

        foreach (var modifier in ModifierKeys(modifiers).Reverse())
        {
            sequence.Add(KeyUp(modifier));
        }

        Send([.. sequence]);
    }

    public void ReleaseModifiers()
    {
        Send(
            KeyUp(NativeInput.VkControl),
            KeyUp(NativeInput.VkShift),
            KeyUp(NativeInput.VkMenu),
            KeyUp(NativeInput.VkLeftWindows));
    }

    private static IEnumerable<ushort> ModifierKeys(KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            yield return NativeInput.VkControl;
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            yield return NativeInput.VkShift;
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            yield return NativeInput.VkMenu;
        }

        if (modifiers.HasFlag(KeyModifiers.Windows))
        {
            yield return NativeInput.VkLeftWindows;
        }
    }

    private static void SendKeySequence(ushort virtualKey)
    {
        Send(KeyDown(virtualKey), KeyUp(virtualKey));
    }

    private static Input KeyDown(ushort virtualKey) => new()
    {
        type = NativeInput.InputKeyboard,
        u = new InputUnion { Keyboard = new KeyboardInput { wVk = virtualKey } }
    };

    private static Input KeyUp(ushort virtualKey) => new()
    {
        type = NativeInput.InputKeyboard,
        u = new InputUnion { Keyboard = new KeyboardInput { wVk = virtualKey, dwFlags = NativeInput.KeyEventKeyUp } }
    };

    private static void Send(params Input[] inputs)
    {
        NativeInput.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }
}
