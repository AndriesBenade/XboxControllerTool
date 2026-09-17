using XboxControllerTool.Application;
using XboxControllerTool.Audio;
using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Notifications;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

/// <summary>
/// Regression cover for a bug where pausing released buttons that were never pressed. Windows raises
/// a context menu on right-button-up, so the stray release popped a menu under the cursor - at launch
/// that landed on the taskbar and trapped the user in it.
/// </summary>
public class HeldInputReleaseTests
{
    [Fact]
    public void PausingWhileNothingIsHeldInjectsNothingAtAll()
    {
        var (controller, mouse, keyboard) = Create();

        controller.ReleaseHeldInputs();

        Assert.Empty(mouse.Actions);
        Assert.Empty(keyboard.Actions);
    }

    [Fact]
    public void PausingAtLaunchNeverSynthesisesARightClick()
    {
        var (controller, mouse, _) = Create();

        // Exactly what happens when the app starts with a yielded surface already focused.
        controller.ReleaseHeldInputs();

        Assert.DoesNotContain("RightButtonUp", mouse.Actions);
        Assert.DoesNotContain("RightButtonDown", mouse.Actions);
    }

    [Theory]
    [InlineData(GamepadButton.A, "LeftButtonUp")]
    [InlineData(GamepadButton.X, "RightButtonUp")]
    [InlineData(GamepadButton.RightThumb, "MiddleButtonUp")]
    public void AMouseButtonGenuinelyHeldIsStillReleasedOnPause(GamepadButton button, string expectedRelease)
    {
        var (controller, mouse, _) = Create();

        controller.Process(State(), Press(button));
        mouse.Actions.Clear();

        controller.ReleaseHeldInputs();

        Assert.Contains(expectedRelease, mouse.Actions);
    }

    [Theory]
    [InlineData(GamepadButton.B, "BackspaceUp")]
    [InlineData(GamepadButton.DPadLeft, "ArrowLeftUp")]
    [InlineData(GamepadButton.DPadRight, "ArrowRightUp")]
    public void AKeyGenuinelyHeldIsStillReleasedOnPause(GamepadButton button, string expectedRelease)
    {
        var (controller, _, keyboard) = Create();

        controller.Process(State(), Press(button));
        keyboard.Actions.Clear();

        controller.ReleaseHeldInputs();

        Assert.Contains(expectedRelease, keyboard.Actions);
    }

    [Fact]
    public void AButtonReleasedNormallyIsNotReleasedASecondTimeOnPause()
    {
        var (controller, mouse, _) = Create();

        controller.Process(State(), Press(GamepadButton.X));
        controller.Process(State(), Release(GamepadButton.X));
        mouse.Actions.Clear();

        controller.ReleaseHeldInputs();

        Assert.Empty(mouse.Actions);
    }

    [Fact]
    public void PausingTwiceOnlyReleasesOnce()
    {
        var (controller, mouse, _) = Create();

        controller.Process(State(), Press(GamepadButton.A));
        mouse.Actions.Clear();

        controller.ReleaseHeldInputs();
        controller.ReleaseHeldInputs();

        Assert.Equal(1, mouse.Actions.Count(action => action == "LeftButtonUp"));
    }

    [Fact]
    public void TheTaskbarIsNotTreatedAsASurfaceWorthYieldingTo()
    {
        var detector = new ShellGamepadSurfaceDetector();

        // Process id 0 forces the window-class branch, which is the only way the taskbar was matched.
        Assert.False(detector.Classify(0, nint.Zero).IsGamepadNavigable);
    }

    private static (DesktopInputController Controller, RecordingMouse Mouse, RecordingKeyboard Keyboard) Create()
    {
        var settings = AppSettings.CreateDefault();
        var mouse = new RecordingMouse();
        var keyboard = new RecordingKeyboard();

        var controller = new DesktopInputController(
            settings,
            mouse,
            keyboard,
            new AlwaysOpensKeyboard(),
            new NoVoiceTypingPanel(),
            new DefaultBrowserController(keyboard, settings),
            new SilentNotifications(),
            new SilentAudio());

        return (controller, mouse, keyboard);
    }

    private static ControllerState State() => new(0, true, 0, GamepadButton.None, 0, 0, 0, 0, 0, 0);

    private static ButtonTransitions Press(GamepadButton button) => new(button, GamepadButton.None);

    private static ButtonTransitions Release(GamepadButton button) => new(GamepadButton.None, button);

    private sealed class NoVoiceTypingPanel : IVoiceTypingFlyout
    {
        public bool IsOpen => false;

        public void ForceCloseIfStillOpen()
        {
        }
    }

    private sealed class AlwaysOpensKeyboard : IOnScreenKeyboardLauncher
    {
        public bool TryOpen() => true;
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
