using XboxControllerTool.Application;
using XboxControllerTool.Audio;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.ConsoleUi.Screens;
using XboxControllerTool.Core;
using XboxControllerTool.Diagnostics;
using XboxControllerTool.Input;
using XboxControllerTool.Notifications;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;
using XboxControllerTool.Windows.RawInput;

TryConfigureConsole();
Console.Title = "XboxControllerTool";

var errors = ErrorReporter.CreateDefault();

// Nothing may reach the default unhandled-exception handler, which prints an uncoloured stack
// trace and kills the process before the user can read it.
AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    if (args.ExceptionObject is Exception exception)
    {
        errors.ReportFatal("unexpected failure", exception);
    }
};

var settingsRepository = SettingsRepository.CreateDefault();
var settings = settingsRepository.Load();

var selectionService = new ControllerSelectionService();
RestoreControllerSelection(selectionService, settings);
selectionService.SelectionChanged += (_, _) => PersistControllerSelection(selectionService, settings, settingsRepository);

var controllerManager = new ControllerManager();
var mouse = new MouseSimulator();
var keyboard = new KeyboardSimulator();
var onScreenKeyboard = new OnScreenKeyboardLauncher();
var defaultBrowser = new DefaultBrowserController(keyboard, settings);

using var notificationManager = new NotificationManager(settings);
var audioFeedbackPlayer = new AudioFeedbackPlayer(settings);

var desktopInput = new DesktopInputController(settings, mouse, keyboard, onScreenKeyboard, defaultBrowser, notificationManager, audioFeedbackPlayer);

var consoleWindow = new ConsoleWindowController();
var fontController = new ConsoleFontController();
var startupManager = new StartupManager();
var uiPreferences = new UiPreferences(settings, settingsRepository, fontController, consoleWindow, startupManager);
uiPreferences.ApplySavedPreferences();

var windowPlacementStore = new WindowPlacementStore();
windowPlacementStore.ApplySize(consoleWindow.Handle, settings.Window);
consoleWindow.CenterOnScreen();
consoleWindow.Minimize();

var appState = new AppState();
var foregroundWindowWatcher = new ForegroundWindowWatcher();
var gameFocusMonitor = new GameFocusMonitor(foregroundWindowWatcher, new GameDetector());
var shellNavigationMonitor = new ShellNavigationMonitor(foregroundWindowWatcher, new ShellGamepadSurfaceDetector());
using var rawGamepadWatcher = new RawGamepadWatcher();
rawGamepadWatcher.Start();

var customButtons = new CustomButtonService(settings, settingsRepository, keyboard, rawGamepadWatcher);

var statusScreen = new StatusScreen(appState, settings);
var customButtonsScreen = new CustomButtonsScreen(customButtons);
var settingsScreen = new SettingsScreen(settings, settingsRepository, uiPreferences, customButtonsScreen);
var controllerSelectionScreen = new ControllerSelectionScreen(selectionService, appState);

var homeScreen = new HomeScreen(appState, customButtons,
[
    new MenuDestination("SETTINGS", "Speed, dead zones and alerts", settingsScreen),
    new MenuDestination("CONTROLLER", "Choose which pad drives the desktop", controllerSelectionScreen),
    new MenuDestination("STATUS", "Full input and configuration detail", statusScreen),
    new MenuDestination("EXIT", "Close XboxControllerTool", null)
]);

var navigator = new ScreenNavigator(homeScreen);
uiPreferences.SurfaceInvalidated += () => navigator.Render(force: true);

var appLoop = new AppLoop(
    controllerManager,
    selectionService,
    desktopInput,
    consoleWindow,
    navigator,
    notificationManager,
    gameFocusMonitor,
    shellNavigationMonitor,
    customButtons,
    settings,
    appState,
    errors);

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cancellationTokenSource.Cancel();
};

var exitCode = 0;

try
{
    await appLoop.RunAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
}
catch (Exception ex)
{
    errors.ReportFatal("the application stopped", ex);
    exitCode = 1;
}
finally
{
    Shutdown();
}

return exitCode;

void Shutdown()
{
    // Each step is independent: a failure in one must not skip the others, and releasing held
    // modifier keys matters most because they would otherwise stay stuck down after exit.
    RunSafely("releasing held keys", customButtons.ReleaseModifiers);
    RunSafely("saving window placement", () => settings.Window = windowPlacementStore.Capture(consoleWindow.Handle));
    RunSafely("saving settings", () => settingsRepository.Save(settings));
    RunSafely("restoring the cursor", () => Console.CursorVisible = true);
}

void RunSafely(string context, Action step)
{
    try
    {
        step();
    }
    catch (Exception ex)
    {
        errors.Report(context, ex);
    }
}

static void TryConfigureConsole()
{
    try
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
    }
    catch (Exception ex) when (ex is ArgumentOutOfRangeException or PlatformNotSupportedException or IOException)
    {
    }
}

static void RestoreControllerSelection(ControllerSelectionService selectionService, AppSettings settings)
{
    if (settings.ControllerMode == ControllerSelectionMode.SpecificController &&
        settings.SelectedControllerUserIndex is { } userIndex &&
        settings.SelectedControllerCapabilityType is { } capType &&
        settings.SelectedControllerCapabilitySubType is { } capSubType &&
        settings.SelectedControllerCapabilityFlags is { } capFlags)
    {
        var identity = new ControllerIdentity(userIndex, new ControllerCapabilities(capType, capSubType, capFlags));
        selectionService.Restore(ControllerSelectionMode.SpecificController, identity);
    }
    else
    {
        selectionService.Restore(ControllerSelectionMode.AllControllers, null);
    }
}

static void PersistControllerSelection(ControllerSelectionService selectionService, AppSettings settings, SettingsRepository repository)
{
    settings.ControllerMode = selectionService.Mode;

    if (selectionService.SelectedIdentity is { } identity)
    {
        settings.SelectedControllerUserIndex = identity.UserIndex;
        settings.SelectedControllerCapabilityType = identity.Capabilities.Type;
        settings.SelectedControllerCapabilitySubType = identity.Capabilities.SubType;
        settings.SelectedControllerCapabilityFlags = identity.Capabilities.Flags;
    }
    else
    {
        settings.SelectedControllerUserIndex = null;
        settings.SelectedControllerCapabilityType = null;
        settings.SelectedControllerCapabilitySubType = null;
        settings.SelectedControllerCapabilityFlags = null;
    }

    repository.Save(settings);
}
