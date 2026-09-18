using XboxControllerTool.Application;
using XboxControllerTool.Audio;
using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Notifications;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

/// <summary>
/// Holding LT hands the D-pad back to Windows, whose own gamepad navigation reads it directly, so
/// anything injected here would be acted on twice in a menu that already responds to the D-pad.
/// </summary>
public class PrecisionDPadSuppressionTests
{
    private const byte TriggerHeld = 255;
    private const byte TriggerReleased = 0;

    [Theory]
    [InlineData(GamepadButton.DPadLeft)]
    [InlineData(GamepadButton.DPadRight)]
    public void ADirectionSendsNoArrowKeyWhilePrecisionModeIsHeld(GamepadButton direction)
    {
        var (controller, _, keyboard, _) = Create();

        Engage(controller);
        keyboard.Actions.Clear();

        controller.Process(State(leftTrigger: TriggerHeld), Press(direction));

        Assert.Empty(keyboard.Actions);
    }

    [Fact]
    public void DPadUpDoesNotOpenTheOnScreenKeyboardWhilePrecisionModeIsHeld()
    {
        var (controller, _, _, keyboard) = Create();

        Engage(controller);

        controller.Process(State(leftTrigger: TriggerHeld), Press(GamepadButton.DPadUp));

        Assert.Equal(0, keyboard.OpenCount);
    }

    [Fact]
    public void DPadDownDoesNotToggleVoiceInputWhilePrecisionModeIsHeld()
    {
        var (controller, _, _, _) = Create();

        Engage(controller);

        controller.Process(State(leftTrigger: TriggerHeld), Press(GamepadButton.DPadDown));

        Assert.False(controller.IsVoiceInputActive);
    }

    [Theory]
    [InlineData(GamepadButton.DPadLeft, "ArrowLeftUp")]
    [InlineData(GamepadButton.DPadRight, "ArrowRightUp")]
    public void AnArrowKeyAlreadyHeldIsReleasedWhenPrecisionModeEngages(GamepadButton direction, string expectedRelease)
    {
        var (controller, _, keyboard, _) = Create();

        controller.Process(State(), Press(direction));
        keyboard.Actions.Clear();

        // LT goes down while the direction is still being held.
        controller.Process(State(leftTrigger: TriggerHeld), Nothing());

        Assert.Contains(expectedRelease, keyboard.Actions);
    }

    [Fact]
    public void AKeyReleasedByPrecisionModeIsNotReleasedAgain()
    {
        var (controller, _, keyboard, _) = Create();

        controller.Process(State(), Press(GamepadButton.DPadLeft));
        controller.Process(State(leftTrigger: TriggerHeld), Nothing());
        keyboard.Actions.Clear();

        controller.Process(State(leftTrigger: TriggerHeld), Nothing());
        controller.Process(State(leftTrigger: TriggerHeld), Release(GamepadButton.DPadLeft));

        Assert.Empty(keyboard.Actions);
    }

    [Theory]
    [InlineData(GamepadButton.DPadLeft, "ArrowLeftDown")]
    [InlineData(GamepadButton.DPadRight, "ArrowRightDown")]
    public void DirectionsWorkAgainOncePrecisionModeIsReleased(GamepadButton direction, string expectedPress)
    {
        var (controller, _, keyboard, _) = Create();

        Engage(controller);
        controller.Process(State(leftTrigger: TriggerReleased), Nothing());
        keyboard.Actions.Clear();

        controller.Process(State(), Press(direction));

        Assert.Contains(expectedPress, keyboard.Actions);
    }

    [Fact]
    public void PrecisionModeStillSlowsTheCursorWhileSuppressingTheDPad()
    {
        var (controller, mouse, _, _) = Create();
        var settings = AppSettings.CreateDefault();

        Engage(controller);

        // A fully deflected stick still moves the cursor; only the D-pad is handed back.
        for (var i = 0; i < 20; i++)
        {
            controller.Process(State(leftTrigger: TriggerHeld, leftThumbX: short.MaxValue), Nothing());
        }

        Assert.Contains("MoveRelative", mouse.Actions);
        Assert.True(settings.PrecisionMultiplier < 1.0);
    }

    private static void Engage(DesktopInputController controller)
    {
        controller.Process(State(leftTrigger: TriggerHeld), Nothing());
        Assert.True(controller.IsPrecisionModeActive);
    }

    private static (DesktopInputController Controller, RecordingMouse Mouse, RecordingKeyboard Keyboard, CountingOnScreenKeyboard Osk) Create()
    {
        var settings = AppSettings.CreateDefault();
        var mouse = new RecordingMouse();
        var keyboard = new RecordingKeyboard();
        var osk = new CountingOnScreenKeyboard();

        var controller = new DesktopInputController(
            settings,
            mouse,
            keyboard,
            osk,
            new NoVoiceTypingPanel(),
            new DefaultBrowserController(keyboard, settings),
            new SilentNotifications(),
            new SilentAudio());

        return (controller, mouse, keyboard, osk);
    }

    private static ControllerState State(byte leftTrigger = 0, short leftThumbX = 0) =>
        new(0, true, 0, GamepadButton.None, leftThumbX, 0, 0, 0, leftTrigger, 0);

    private static ButtonTransitions Press(GamepadButton button) => new(button, GamepadButton.None);

    private static ButtonTransitions Release(GamepadButton button) => new(GamepadButton.None, button);

    private static ButtonTransitions Nothing() => new(GamepadButton.None, GamepadButton.None);

    private sealed class CountingOnScreenKeyboard : IOnScreenKeyboardLauncher
    {
        public int OpenCount { get; private set; }

        public bool TryOpen()
        {
            OpenCount++;
            return true;
        }
    }

    private sealed class NoVoiceTypingPanel : IVoiceTypingFlyout
    {
        public bool IsOpen => false;

        public void ForceCloseIfStillOpen()
        {
        }
    }

    private sealed class SilentNotifications : INotificationService
    {
        public void ShowTransient(string title, string? subtitle = null, NotificationKind kind = NotificationKind.Info)
        {
        }
    }

    private sealed class SilentAudio : IAudioFeedbackPlayer
    {
        public void PlayVoiceInputStarted()
        {
        }

        public void PlayVoiceInputStopped()
        {
        }
    }
}
