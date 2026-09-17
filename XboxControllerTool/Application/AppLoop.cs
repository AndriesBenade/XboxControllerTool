using XboxControllerTool.ConsoleUi;
using XboxControllerTool.Core;
using XboxControllerTool.Input;
using XboxControllerTool.Notifications;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Application;

public sealed class AppLoop
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(8);
    private static readonly TimeSpan MenuRefreshInterval = TimeSpan.FromMilliseconds(100);
    private const int XInputSlotCount = 4;

    private readonly ControllerManager _controllerManager;
    private readonly ControllerSelectionService _selectionService;
    private readonly DesktopInputController _desktopInput;
    private readonly ConsoleWindowController _consoleWindow;
    private readonly ScreenNavigator _navigator;
    private readonly INotificationService _notifications;
    private readonly AppState _appState;

    private readonly GamepadButton[] _previousSlotButtons = new GamepadButton[XInputSlotCount];
    private readonly bool[] _previousConnected = new bool[XInputSlotCount];
    private GamepadButton _previousRawCombinedButtons;
    private GamepadButton _previousMenuDirections;
    private InputContext _context = InputContext.DesktopControl;
    private DateTime _lastMenuRenderUtc = DateTime.MinValue;
    private bool? _observedMinimized;

    public AppLoop(
        ControllerManager controllerManager,
        ControllerSelectionService selectionService,
        DesktopInputController desktopInput,
        ConsoleWindowController consoleWindow,
        ScreenNavigator navigator,
        INotificationService notifications,
        AppState appState)
    {
        _controllerManager = controllerManager;
        _selectionService = selectionService;
        _desktopInput = desktopInput;
        _consoleWindow = consoleWindow;
        _navigator = navigator;
        _notifications = notifications;
        _appState = appState;

        _selectionService.SelectionChanged += OnSelectionChanged;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.CursorVisible = false;
        _navigator.Render(force: true);

        using var timer = new PeriodicTimer(PollInterval);
        while (!_navigator.ShouldExit && await timer.WaitForNextTickAsync(cancellationToken))
        {
            Tick();
        }
    }

    private void Tick()
    {
        var snapshots = _controllerManager.Poll();
        var slotTransitions = new ButtonTransitions[snapshots.Length];

        for (var i = 0; i < snapshots.Length; i++)
        {
            slotTransitions[i] = ButtonEdgeDetector.Detect(_previousSlotButtons[i], snapshots[i].State.Buttons);
            _previousSlotButtons[i] = snapshots[i].State.Buttons;

            var isConnected = snapshots[i].State.IsConnected;
            _appState.SlotConnected[i] = isConnected;

            if (isConnected != _previousConnected[i])
            {
                _previousConnected[i] = isConnected;
                _notifications.ShowTransient(isConnected ? "Controller Connected" : "Controller Disconnected");
            }
        }

        _selectionService.ProcessSnapshots(snapshots, slotTransitions);

        _appState.ConnectedControllerCount = snapshots.Count(s => s.State.IsConnected);
        _appState.ControllerMode = _selectionService.Mode;
        _appState.SelectedControllerUserIndex = _selectionService.SelectedIdentity?.UserIndex;
        _appState.SelectedControllerAvailability = _selectionService.GetSelectedControllerAvailability(snapshots);

        var accepted = _selectionService.FilterAccepted(snapshots);
        var combined = ControllerStateAggregator.Combine(accepted);

        var rawTransitions = ButtonEdgeDetector.Detect(_previousRawCombinedButtons, combined.Buttons);
        _previousRawCombinedButtons = combined.Buttons;

        if (rawTransitions.WasPressed(GamepadButton.Y))
        {
            if (_context == InputContext.DesktopControl)
            {
                ShowApp(combined);
            }
            else
            {
                HideApp(restorePreviousForeground: true);
            }
        }

        SyncWithWindowState(combined);

        if (_context == InputContext.DesktopControl)
        {
            _desktopInput.Process(combined, rawTransitions);
            _appState.PrecisionModeActive = _desktopInput.IsPrecisionModeActive;
            _appState.SpeedBoostActive = _desktopInput.IsSpeedBoostActive;
            _appState.VoiceInputActive = _desktopInput.IsVoiceInputActive;
        }
        else
        {
            var directions = MenuInputTranslator.ComputeDigitalDirections(combined);
            var directionTransitions = ButtonEdgeDetector.Detect(_previousMenuDirections, directions);
            _previousMenuDirections = directions;

            var action = MenuInputTranslator.ToPrimaryAction(rawTransitions, directionTransitions.Pressed);
            _navigator.Dispatch(action);

            var now = DateTime.UtcNow;
            if (action != MenuAction.None || now - _lastMenuRenderUtc >= MenuRefreshInterval)
            {
                _lastMenuRenderUtc = now;
                _navigator.Render();
            }
        }
    }

    private void SyncWithWindowState(ControllerState combined)
    {
        var isMinimized = _consoleWindow.IsMinimized;

        if (_observedMinimized is null)
        {
            _observedMinimized = isMinimized;
            return;
        }

        if (isMinimized == _observedMinimized)
        {
            return;
        }

        _observedMinimized = isMinimized;

        if (isMinimized && _context == InputContext.MenuNavigation)
        {
            HideApp(restorePreviousForeground: false);
        }
        else if (!isMinimized && _context == InputContext.DesktopControl)
        {
            ShowApp(combined);
        }
    }

    private void ShowApp(ControllerState combined)
    {
        _context = InputContext.MenuNavigation;
        _previousMenuDirections = MenuInputTranslator.ComputeDigitalDirections(combined);
        _consoleWindow.BringToForegroundAndTopMost();
        _appState.ConsoleTopMost = true;
        _notifications.ShowTransient("App Shown", "Press Y again to hide");
        _navigator.Render(force: true);
    }

    private void HideApp(bool restorePreviousForeground)
    {
        _context = InputContext.DesktopControl;
        _consoleWindow.Hide(restorePreviousForeground);
        _appState.ConsoleTopMost = false;
        _desktopInput.ResetMotionState();
        _notifications.ShowTransient("App Hidden");
    }

    private void OnSelectionChanged(object? sender, ControllerSelectionChangedEventArgs e)
    {
        _notifications.ShowTransient(
            e.Mode == ControllerSelectionMode.AllControllers
                ? "All Controllers"
                : "Controller Selected");
    }
}
