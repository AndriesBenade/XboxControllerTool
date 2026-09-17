using XboxControllerTool.Application;
using XboxControllerTool.Audio;
using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Notifications;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

public class VoiceTypingAndMiddleClickTests
{
    [Fact]
    public void ClickingTheRightStickPressesTheMiddleMouseButton()
    {
        var (controller, mouse, _, _) = Create();

        controller.Process(State(), Press(GamepadButton.RightThumb));

        Assert.Contains("MiddleButtonDown", mouse.Actions);
        Assert.DoesNotContain("MiddleButtonUp", mouse.Actions);
    }

    [Fact]
    public void ReleasingTheRightStickReleasesTheMiddleMouseButton()
    {
        var (controller, mouse, _, _) = Create();

        controller.Process(State(), Press(GamepadButton.RightThumb));
        mouse.Actions.Clear();
        controller.Process(State(), Release(GamepadButton.RightThumb));

        Assert.Contains("MiddleButtonUp", mouse.Actions);
    }

    [Fact]
    public void HoldingTheRightStickDoesNotRepeatTheMiddleClick()
    {
        var (controller, mouse, _, _) = Create();

        controller.Process(State(), Press(GamepadButton.RightThumb));

        for (var i = 0; i < 50; i++)
        {
            controller.Process(State(), Nothing());
        }

        Assert.Equal(1, mouse.Actions.Count(action => action == "MiddleButtonDown"));
    }

    [Fact]
    public void APausedSessionReleasesAHeldMiddleButton()
    {
        var (controller, mouse, _, _) = Create();

        controller.Process(State(), Press(GamepadButton.RightThumb));
        mouse.Actions.Clear();
        controller.ReleaseHeldInputs();

        Assert.Contains("MiddleButtonUp", mouse.Actions);
    }

    [Fact]
    public void TheRightStickClickIsNoLongerOfferedForCustomMappingBecauseItNowHasAnAction()
    {
        Assert.False(CustomButtonService.IsMappable((ushort)GamepadButton.RightThumb));
        Assert.False(CustomButtonService.IsMappable(ButtonIds.ForXInput((ushort)GamepadButton.RightThumb)));
    }

    [Fact]
    public void StoppingVoiceInputForcesTheWindowsPanelToClose()
    {
        var (controller, _, keyboard, flyout) = Create();

        controller.Process(State(), Press(GamepadButton.DPadDown));
        Assert.Equal(0, flyout.ForceCloseCalls);

        controller.Process(State(), Press(GamepadButton.DPadDown));

        Assert.False(controller.IsVoiceInputActive);
        Assert.Equal(1, flyout.ForceCloseCalls);
        Assert.Contains("StopVoiceTyping", keyboard.Actions);
    }

    [Fact]
    public void StartingVoiceInputNeverTriesToCloseThePanel()
    {
        var (controller, _, _, flyout) = Create();

        controller.Process(State(), Press(GamepadButton.DPadDown));

        Assert.True(controller.IsVoiceInputActive);
        Assert.Equal(0, flyout.ForceCloseCalls);
    }

    [Fact]
    public void TheRealPanelReportsClosedWhenVoiceTypingIsNotRunning()
    {
        var flyout = new VoiceTypingFlyout();

        // Exercises the real window lookup on this machine. The panel window exists but is DWM
        // cloaked while nothing is on screen, which is exactly the case a visibility check gets wrong.
        Assert.False(flyout.IsOpen);

        flyout.ForceCloseIfStillOpen();
    }

    private static (DesktopInputController Controller, RecordingMouse Mouse, RecordingKeyboard Keyboard, FakeVoiceTypingFlyout Flyout) Create()
    {
        var settings = AppSettings.CreateDefault();
        var mouse = new RecordingMouse();
        var keyboard = new RecordingKeyboard();
        var flyout = new FakeVoiceTypingFlyout();

        var controller = new DesktopInputController(
            settings,
            mouse,
            keyboard,
            new FakeOnScreenKeyboard(),
            flyout,
            new DefaultBrowserController(keyboard, settings),
            new SilentNotifications(),
            new SilentAudio());

        return (controller, mouse, keyboard, flyout);
    }

    private static ControllerState State() => new(0, true, 0, GamepadButton.None, 0, 0, 0, 0, 0, 0);

    private static ButtonTransitions Press(GamepadButton button) => new(button, GamepadButton.None);

    private static ButtonTransitions Release(GamepadButton button) => new(GamepadButton.None, button);

    private static ButtonTransitions Nothing() => new(GamepadButton.None, GamepadButton.None);

    private sealed class FakeVoiceTypingFlyout : IVoiceTypingFlyout
    {
        public int ForceCloseCalls { get; private set; }

        public bool IsOpen { get; set; }

        public void ForceCloseIfStillOpen() => ForceCloseCalls++;
    }

    private sealed class FakeOnScreenKeyboard : IOnScreenKeyboardLauncher
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
