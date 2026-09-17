using XboxControllerTool.Audio;
using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Notifications;
using XboxControllerTool.Processing;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Application;

public sealed class DesktopInputController
{
    private readonly AppSettings _settings;
    private readonly IMouseInput _mouse;
    private readonly IKeyboardInput _keyboard;
    private readonly IOnScreenKeyboardLauncher _onScreenKeyboard;
    private readonly IVoiceTypingFlyout _voiceTypingFlyout;
    private readonly DefaultBrowserController _browser;
    private readonly INotificationService _notifications;
    private readonly IAudioFeedbackPlayer _audio;
    private readonly MouseMovementProcessor _movementProcessor = new();
    private readonly ScrollProcessor _scrollProcessor = new();
    private readonly TriggerHoldTracker _precisionTracker = new();
    private readonly TriggerHoldTracker _enterTracker = new();

    private bool _leftButtonHeld;
    private bool _rightButtonHeld;
    private bool _middleButtonHeld;
    private bool _backspaceHeld;
    private bool _arrowLeftHeld;
    private bool _arrowRightHeld;

    public DesktopInputController(
        AppSettings settings,
        IMouseInput mouse,
        IKeyboardInput keyboard,
        IOnScreenKeyboardLauncher onScreenKeyboard,
        IVoiceTypingFlyout voiceTypingFlyout,
        DefaultBrowserController browser,
        INotificationService notifications,
        IAudioFeedbackPlayer audio)
    {
        _settings = settings;
        _mouse = mouse;
        _keyboard = keyboard;
        _onScreenKeyboard = onScreenKeyboard;
        _voiceTypingFlyout = voiceTypingFlyout;
        _browser = browser;
        _notifications = notifications;
        _audio = audio;
    }

    public bool IsPrecisionModeActive => _precisionTracker.IsActive;

    public bool IsVoiceInputActive { get; private set; }

    public void ResetMotionState()
    {
        _movementProcessor.Reset();
        _scrollProcessor.Reset();
    }

    /// <summary>
    /// Releases only what is genuinely held down.
    /// <para>
    /// Releasing unconditionally injected a mouse-up with no matching mouse-down, and Windows raises
    /// a context menu on right-button-up, so a pause while nothing was held popped a context menu
    /// under the cursor. At launch that landed on the taskbar and left the user stuck in it.
    /// </para>
    /// </summary>
    public void ReleaseHeldInputs()
    {
        if (_leftButtonHeld)
        {
            _mouse.LeftButtonUp();
            _leftButtonHeld = false;
        }

        if (_rightButtonHeld)
        {
            _mouse.RightButtonUp();
            _rightButtonHeld = false;
        }

        if (_middleButtonHeld)
        {
            _mouse.MiddleButtonUp();
            _middleButtonHeld = false;
        }

        if (_backspaceHeld)
        {
            _keyboard.BackspaceUp();
            _backspaceHeld = false;
        }

        if (_arrowLeftHeld)
        {
            _keyboard.ArrowLeftUp();
            _arrowLeftHeld = false;
        }

        if (_arrowRightHeld)
        {
            _keyboard.ArrowRightUp();
            _arrowRightHeld = false;
        }

        ResetMotionState();
    }

    public void Process(ControllerState state, ButtonTransitions transitions)
    {
        var precisionTransition = _precisionTracker.Update(state.LeftTrigger);
        if (precisionTransition == TriggerHoldTransition.Activated)
        {
            _notifications.ShowTransient("Precision Mode: ON", "Release LT to disable");
        }
        else if (precisionTransition == TriggerHoldTransition.Deactivated)
        {
            _notifications.ShowTransient("Precision Mode: OFF");
        }

        if (_enterTracker.Update(state.RightTrigger) == TriggerHoldTransition.Activated)
        {
            _keyboard.SendEnter();
        }

        var mouseSpeedMultiplier = _precisionTracker.IsActive ? _settings.PrecisionMultiplier : 1.0;
        var scrollSpeedMultiplier = _precisionTracker.IsActive ? _settings.ScrollPrecisionMultiplier : 1.0;

        var processedStick = AnalogStickProcessor.Process(
            state.LeftThumbX, state.LeftThumbY, _settings.StickDeadZone, _settings.MouseAccelerationEnabled);

        var (dx, dy) = _movementProcessor.ComputeMovement(processedStick, _settings.MouseSensitivity, mouseSpeedMultiplier);
        _mouse.MoveRelative(dx, dy);

        var wheelDelta = _scrollProcessor.ComputeWheelDelta(state.RightThumbY, _settings.ScrollDeadZone, _settings.ScrollSensitivity, scrollSpeedMultiplier);
        _mouse.Scroll(wheelDelta);

        if (transitions.WasPressed(GamepadButton.A))
        {
            _mouse.LeftButtonDown();
            _leftButtonHeld = true;
        }

        if (transitions.WasReleased(GamepadButton.A))
        {
            _mouse.LeftButtonUp();
            _leftButtonHeld = false;
        }

        if (transitions.WasPressed(GamepadButton.RightThumb))
        {
            _mouse.MiddleButtonDown();
            _middleButtonHeld = true;
        }

        if (transitions.WasReleased(GamepadButton.RightThumb))
        {
            _mouse.MiddleButtonUp();
            _middleButtonHeld = false;
        }

        if (transitions.WasPressed(GamepadButton.X))
        {
            _mouse.RightButtonDown();
            _rightButtonHeld = true;
        }

        if (transitions.WasReleased(GamepadButton.X))
        {
            _mouse.RightButtonUp();
            _rightButtonHeld = false;
        }

        if (transitions.WasPressed(GamepadButton.B))
        {
            _keyboard.BackspaceDown();
            _backspaceHeld = true;
        }

        if (transitions.WasReleased(GamepadButton.B))
        {
            _keyboard.BackspaceUp();
            _backspaceHeld = false;
        }

        if (transitions.WasPressed(GamepadButton.Start))
        {
            var result = _browser.ActivateOrNavigateForward();
            if (result == BrowserActivationResult.Opened)
            {
                _notifications.ShowTransient("Browser Opened");
            }
            else if (result == BrowserActivationResult.Focused)
            {
                _notifications.ShowTransient("Browser Focused");
            }
        }

        if (transitions.WasPressed(GamepadButton.Back))
        {
            _keyboard.SendAltLeft();
        }

        if (transitions.WasPressed(GamepadButton.LeftShoulder))
        {
            _keyboard.SendShowDesktop();
            _notifications.ShowTransient("Show Desktop");
        }

        if (transitions.WasPressed(GamepadButton.RightShoulder))
        {
            _keyboard.SendEscape();
        }

        if (transitions.WasPressed(GamepadButton.DPadLeft))
        {
            _keyboard.ArrowLeftDown();
            _arrowLeftHeld = true;
        }

        if (transitions.WasReleased(GamepadButton.DPadLeft))
        {
            _keyboard.ArrowLeftUp();
            _arrowLeftHeld = false;
        }

        if (transitions.WasPressed(GamepadButton.DPadRight))
        {
            _keyboard.ArrowRightDown();
            _arrowRightHeld = true;
        }

        if (transitions.WasReleased(GamepadButton.DPadRight))
        {
            _keyboard.ArrowRightUp();
            _arrowRightHeld = false;
        }

        if (transitions.WasPressed(GamepadButton.DPadUp))
        {
            var opened = _onScreenKeyboard.TryOpen();
            _notifications.ShowTransient(opened ? "OSK Opened" : "OSK Failed");
        }

        if (transitions.WasPressed(GamepadButton.DPadDown))
        {
            ToggleVoiceInput();
        }
    }

    private void ToggleVoiceInput()
    {
        IsVoiceInputActive = !IsVoiceInputActive;

        if (IsVoiceInputActive)
        {
            _keyboard.StartVoiceTyping();
            _notifications.ShowTransient("Voice Input: ACTIVE", "Press DOWN to stop");
            _audio.PlayVoiceInputStarted();
        }
        else
        {
            _keyboard.StopVoiceTyping();

            // The Win+H toggle does not always dismiss the panel, so its own window is closed if it
            // is still up shortly after. Anything already dictated stays in the field.
            _voiceTypingFlyout.ForceCloseIfStillOpen();

            _notifications.ShowTransient("Voice Input: OFF");
            _audio.PlayVoiceInputStopped();
        }
    }
}
