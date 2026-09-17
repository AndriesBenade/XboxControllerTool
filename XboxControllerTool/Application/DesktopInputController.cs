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
    private readonly DefaultBrowserController _browser;
    private readonly INotificationService _notifications;
    private readonly IAudioFeedbackPlayer _audio;
    private readonly MouseMovementProcessor _movementProcessor = new();
    private readonly ScrollProcessor _scrollProcessor = new();
    private readonly TriggerHoldTracker _precisionTracker = new();
    private readonly TriggerHoldTracker _boostTracker = new();

    public DesktopInputController(
        AppSettings settings,
        IMouseInput mouse,
        IKeyboardInput keyboard,
        IOnScreenKeyboardLauncher onScreenKeyboard,
        DefaultBrowserController browser,
        INotificationService notifications,
        IAudioFeedbackPlayer audio)
    {
        _settings = settings;
        _mouse = mouse;
        _keyboard = keyboard;
        _onScreenKeyboard = onScreenKeyboard;
        _browser = browser;
        _notifications = notifications;
        _audio = audio;
    }

    public bool IsPrecisionModeActive => _precisionTracker.IsActive;

    public bool IsSpeedBoostActive => _boostTracker.IsActive;

    public bool IsVoiceInputActive { get; private set; }

    public void ResetMotionState()
    {
        _movementProcessor.Reset();
        _scrollProcessor.Reset();
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

        var boostTransition = _boostTracker.Update(state.RightTrigger);
        if (boostTransition == TriggerHoldTransition.Activated)
        {
            _notifications.ShowTransient("Fast Mode: ON", "Release RT to disable");
        }
        else if (boostTransition == TriggerHoldTransition.Deactivated)
        {
            _notifications.ShowTransient("Fast Mode: OFF");
        }

        var mouseSpeedMultiplier = _precisionTracker.IsActive
            ? _settings.PrecisionMultiplier
            : _boostTracker.IsActive
                ? _settings.BoostMultiplier
                : 1.0;

        var scrollSpeedMultiplier = _precisionTracker.IsActive
            ? _settings.ScrollPrecisionMultiplier
            : _boostTracker.IsActive
                ? _settings.BoostMultiplier
                : 1.0;

        var processedStick = AnalogStickProcessor.Process(
            state.LeftThumbX, state.LeftThumbY, _settings.StickDeadZone, _settings.MouseAccelerationEnabled);

        var (dx, dy) = _movementProcessor.ComputeMovement(processedStick, _settings.MouseSensitivity, mouseSpeedMultiplier);
        _mouse.MoveRelative(dx, dy);

        var wheelDelta = _scrollProcessor.ComputeWheelDelta(state.RightThumbY, _settings.ScrollDeadZone, _settings.ScrollSensitivity, scrollSpeedMultiplier);
        _mouse.Scroll(wheelDelta);

        if (transitions.WasPressed(GamepadButton.A))
        {
            _mouse.LeftButtonDown();
        }

        if (transitions.WasReleased(GamepadButton.A))
        {
            _mouse.LeftButtonUp();
        }

        if (transitions.WasPressed(GamepadButton.X))
        {
            _mouse.RightButtonDown();
        }

        if (transitions.WasReleased(GamepadButton.X))
        {
            _mouse.RightButtonUp();
        }

        if (transitions.WasPressed(GamepadButton.B))
        {
            _keyboard.BackspaceDown();
        }

        if (transitions.WasReleased(GamepadButton.B))
        {
            _keyboard.BackspaceUp();
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

        if (transitions.WasPressed(GamepadButton.LeftThumb))
        {
            _keyboard.SendEnter();
        }

        if (transitions.WasPressed(GamepadButton.DPadLeft))
        {
            _keyboard.ArrowLeftDown();
        }

        if (transitions.WasReleased(GamepadButton.DPadLeft))
        {
            _keyboard.ArrowLeftUp();
        }

        if (transitions.WasPressed(GamepadButton.DPadRight))
        {
            _keyboard.ArrowRightDown();
        }

        if (transitions.WasReleased(GamepadButton.DPadRight))
        {
            _keyboard.ArrowRightUp();
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
            _notifications.ShowTransient("Voice Input: OFF");
            _audio.PlayVoiceInputStopped();
        }
    }
}
