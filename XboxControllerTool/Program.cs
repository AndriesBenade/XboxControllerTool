using XboxControllerTool.Application;
using XboxControllerTool.Audio;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.ConsoleUi.Screens;
using XboxControllerTool.Core;
using XboxControllerTool.Input;
using XboxControllerTool.Notifications;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

TryConfigureConsoleWindow();
Console.Title = "XboxControllerTool";

var settingsRepository = SettingsRepository.CreateDefault();
var settings = settingsRepository.Load();

var selectionService = new ControllerSelectionService();
RestoreControllerSelection(selectionService, settings);
selectionService.SelectionChanged += (_, _) => PersistControllerSelection(selectionService, settings, settingsRepository);

var controllerManager = new ControllerManager();
var mouse = new MouseSimulator();
var keyboard = new KeyboardSimulator();
var onScreenKeyboard = new OnScreenKeyboardLauncher();
var defaultBrowser = new DefaultBrowserController(keyboard);

using var notificationManager = new NotificationManager(settings);
var audioFeedbackPlayer = new AudioFeedbackPlayer(settings);

var desktopInput = new DesktopInputController(settings, mouse, keyboard, onScreenKeyboard, defaultBrowser, notificationManager, audioFeedbackPlayer);

var consoleWindow = new ConsoleWindowController();
var windowPlacementStore = new WindowPlacementStore();
windowPlacementStore.ApplySize(consoleWindow.Handle, settings.Window);
consoleWindow.CenterOnScreen();
consoleWindow.Minimize();

var appState = new AppState();

var statusScreen = new StatusScreen(appState, settings);
var settingsScreen = new SettingsScreen(settings, settingsRepository);
var controllerSelectionScreen = new ControllerSelectionScreen(selectionService, appState);

var homeScreen = new HomeScreen(appState,
[
    new MenuDestination("SETTINGS", "Speed, dead zones and alerts", settingsScreen),
    new MenuDestination("CONTROLLER", "Choose which pad drives the desktop", controllerSelectionScreen),
    new MenuDestination("STATUS", "Full input and configuration detail", statusScreen),
    new MenuDestination("EXIT", "Close XboxControllerTool", null)
]);

var navigator = new ScreenNavigator(homeScreen);
var appLoop = new AppLoop(controllerManager, selectionService, desktopInput, consoleWindow, navigator, notificationManager, appState);

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cancellationTokenSource.Cancel();
};

try
{
    await appLoop.RunAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
}
finally
{
    settings.Window = windowPlacementStore.Capture(consoleWindow.Handle);
    settingsRepository.Save(settings);
    Console.CursorVisible = true;
}

return;

static void TryConfigureConsoleWindow()
{
    try
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.SetWindowSize(Math.Min(84, Console.LargestWindowWidth), Math.Min(38, Console.LargestWindowHeight));
        Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
        Console.BackgroundColor = ConsoleTheme.Background;
        Console.ForegroundColor = ConsoleTheme.Text;
        Console.Clear();
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
