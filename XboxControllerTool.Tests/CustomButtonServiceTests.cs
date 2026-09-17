using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

public class CustomButtonServiceTests : IDisposable
{
    private static readonly string ExtraId = ButtonIds.ForXInput((ushort)GamepadButton.Extra);
    private static readonly string GuideId = ButtonIds.ForXInput((ushort)GamepadButton.Guide);

    private readonly string _directory;
    private readonly string _settingsPath;
    private readonly RecordingKeyboard _keyboard = new();
    private readonly FakeRawGamepadSource _rawSource = new();

    private DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public CustomButtonServiceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "XboxControllerToolButtons_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
        _settingsPath = Path.Combine(_directory, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(GamepadButton.A)]
    [InlineData(GamepadButton.B)]
    [InlineData(GamepadButton.X)]
    [InlineData(GamepadButton.Y)]
    [InlineData(GamepadButton.Start)]
    [InlineData(GamepadButton.Back)]
    [InlineData(GamepadButton.LeftShoulder)]
    [InlineData(GamepadButton.RightShoulder)]
    [InlineData(GamepadButton.DPadUp)]
    [InlineData(GamepadButton.DPadDown)]
    [InlineData(GamepadButton.DPadLeft)]
    [InlineData(GamepadButton.DPadRight)]
    public void ButtonsWithBuiltInActionsAreNeverOfferedForRemapping(GamepadButton button)
    {
        Assert.False(CustomButtonService.IsMappable((ushort)button));
        Assert.False(CustomButtonService.IsMappable(ButtonIds.ForXInput((ushort)button)));
    }

    [Fact]
    public void LeftStickClickIsNotOfferedForRemappingBecauseItIsDeliberatelyUnassigned()
    {
        Assert.False(CustomButtonService.IsMappable((ushort)GamepadButton.LeftThumb));
    }

    [Fact]
    public void NoButtonIsListedUntilOneHasActuallyBeenPressed()
    {
        var service = Create(out _);

        Assert.Empty(service.DetectedButtons);
    }

    [Fact]
    public void PressingASpareButtonAddsItToTheDetectedListAsConfirmed()
    {
        var service = Create(out _);

        service.Process(Press(GamepadButton.Guide), executeActions: true);

        var button = Assert.Single(service.DetectedButtons);
        Assert.Equal(GuideId, button.Id);
        Assert.True(button.Confirmed);
        Assert.True(service.IsConfirmed(GuideId));
    }

    [Fact]
    public void PressingABuiltInButtonNeverAddsItToTheDetectedList()
    {
        var service = Create(out _);

        service.Process(Press(GamepadButton.A), executeActions: true);
        service.Process(Press(GamepadButton.LeftThumb), executeActions: true);

        Assert.Empty(service.DetectedButtons);
    }

    [Fact]
    public void AButtonCannotBeMappedUntilItHasBeenPressedAndDetected()
    {
        var service = Create(out _);

        service.Assign(ExtraId, KeyModifiers.Windows, "D");

        Assert.Equal("Unassigned", service.DescribeMapping(ExtraId));
        Assert.False(service.IsConfirmed(ExtraId));

        service.Process(Press(GamepadButton.Extra), executeActions: true);
        service.Assign(ExtraId, KeyModifiers.Windows, "D");

        Assert.Equal("Win + D", service.DescribeMapping(ExtraId));
    }

    [Fact]
    public void AMappedButtonSendsItsCombinationOncePerPress()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out _);
        service.Assign(ExtraId, KeyModifiers.Windows, "D");

        service.Process(Press(GamepadButton.Extra), executeActions: true);
        service.Process(None(), executeActions: true);
        service.Process(new ButtonTransitions(GamepadButton.None, GamepadButton.Extra), executeActions: true);

        var combination = Assert.Single(_keyboard.Combinations);
        Assert.Equal(KeyModifiers.Windows, combination.Modifiers);
        Assert.Equal(0x44, combination.VirtualKey);
    }

    [Fact]
    public void HoldingAMappedButtonDoesNotRepeatTheCombination()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out _);
        service.Assign(ExtraId, KeyModifiers.Alt, "Tab");

        service.Process(Press(GamepadButton.Extra), executeActions: true);

        for (var i = 0; i < 100; i++)
        {
            service.Process(None(), executeActions: true);
        }

        Assert.Single(_keyboard.Combinations);
    }

    [Fact]
    public void AnUnmappedButtonPressSendsNothing()
    {
        var service = Create(out _);

        service.Process(Press(GamepadButton.Extra), executeActions: true);

        Assert.Empty(_keyboard.Combinations);
    }

    [Fact]
    public void ButtonsAreStillDetectedWhileActionsAreSuppressed()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out _);
        service.Assign(ExtraId, KeyModifiers.Windows, "D");
        _keyboard.Combinations.Clear();

        service.Process(Press(GamepadButton.Guide), executeActions: false);
        service.Process(Press(GamepadButton.Extra), executeActions: false);

        Assert.Contains(service.DetectedButtons, button => button.Id == GuideId);
        Assert.Empty(_keyboard.Combinations);
    }

    [Fact]
    public void MappingsSurviveARestart()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out var repository);
        service.Assign(ExtraId, KeyModifiers.Control | KeyModifiers.Shift, "Esc");

        var reloaded = new CustomButtonService(repository.Load(), repository, _keyboard);

        Assert.Equal("Ctrl + Shift + Esc", reloaded.DescribeMapping(ExtraId));
    }

    [Fact]
    public void ASavedButtonIsListedAfterARestartButMustBePressedAgainBeforeItIsRemapped()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out var repository);
        service.Assign(ExtraId, KeyModifiers.Windows, "D");

        var reloaded = new CustomButtonService(repository.Load(), repository, _keyboard);

        var listed = Assert.Single(reloaded.DetectedButtons);
        Assert.Equal(ExtraId, listed.Id);
        Assert.False(listed.Confirmed);

        reloaded.Assign(ExtraId, KeyModifiers.Alt, "F4");
        Assert.Equal("Win + D", reloaded.DescribeMapping(ExtraId));

        reloaded.Process(Press(GamepadButton.Extra), executeActions: false);
        reloaded.Assign(ExtraId, KeyModifiers.Alt, "F4");
        Assert.Equal("Alt + F4", reloaded.DescribeMapping(ExtraId));
    }

    [Fact]
    public void ASavedMappingStillFiresBeforeTheButtonIsPressedAgain()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out var repository);
        service.Assign(ExtraId, KeyModifiers.Windows, "D");
        _keyboard.Combinations.Clear();

        var reloaded = new CustomButtonService(repository.Load(), repository, _keyboard);
        reloaded.Process(Press(GamepadButton.Extra), executeActions: true);

        Assert.Single(_keyboard.Combinations);
    }

    [Fact]
    public void ClearingAMappingRemovesItAndStopsItFiring()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out _);
        service.Assign(ExtraId, KeyModifiers.Windows, "D");

        service.Clear(ExtraId);
        service.Process(Press(GamepadButton.Extra), executeActions: true);

        Assert.Equal("Unassigned", service.DescribeMapping(ExtraId));
        Assert.Empty(_keyboard.Combinations);
    }

    [Fact]
    public void MappingsForUnknownKeysOrReservedButtonsAreDiscardedOnLoad()
    {
        var settings = AppSettings.CreateDefault();
        settings.CustomButtons.Add(new CustomButtonMapping { ButtonId = ExtraId, Key = "NotARealKey" });
        settings.CustomButtons.Add(new CustomButtonMapping { ButtonId = ButtonIds.ForXInput((ushort)GamepadButton.A), Key = "D" });
        settings.CustomButtons.Add(new CustomButtonMapping { ButtonId = GuideId, Key = "F5" });

        var repository = new SettingsRepository(_settingsPath);
        var service = new CustomButtonService(settings, repository, _keyboard);

        Assert.Equal("Unassigned", service.DescribeMapping(ExtraId));
        Assert.Equal("Unassigned", service.DescribeMapping(ButtonIds.ForXInput((ushort)GamepadButton.A)));
        Assert.Equal("F5", service.DescribeMapping(GuideId));
    }

    [Fact]
    public void MappingsSavedBeforeHidSupportAreMigratedToTheNewIdentityFormat()
    {
        var settings = AppSettings.CreateDefault();
        settings.CustomButtons.Add(new CustomButtonMapping
        {
            Button = (ushort)GamepadButton.Extra,
            Modifiers = KeyModifiers.Windows,
            Key = "D"
        });

        var repository = new SettingsRepository(_settingsPath);
        var service = new CustomButtonService(settings, repository, _keyboard);

        Assert.Equal("Win + D", service.DescribeMapping(ExtraId));

        var saved = Assert.Single(repository.Load().CustomButtons);
        Assert.Equal(ExtraId, saved.ButtonId);
        Assert.Equal(0, saved.Button);
    }

    [Fact]
    public void AssigningIsRejectedForReservedButtonsAndUnknownKeys()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out _);

        service.Assign(ButtonIds.ForXInput((ushort)GamepadButton.A), KeyModifiers.None, "D");
        service.Assign(ButtonIds.ForXInput((ushort)GamepadButton.LeftThumb), KeyModifiers.None, "D");
        service.Assign(ExtraId, KeyModifiers.None, "NotARealKey");

        Assert.Equal("Unassigned", service.DescribeMapping(ButtonIds.ForXInput((ushort)GamepadButton.A)));
        Assert.Equal("Unassigned", service.DescribeMapping(ButtonIds.ForXInput((ushort)GamepadButton.LeftThumb)));
        Assert.Equal("Unassigned", service.DescribeMapping(ExtraId));
    }

    [Fact]
    public void ReassigningAButtonReplacesTheExistingMappingRatherThanAddingASecond()
    {
        var service = CreateConfirmed(GamepadButton.Extra, out var repository);

        service.Assign(ExtraId, KeyModifiers.Windows, "D");
        service.Assign(ExtraId, KeyModifiers.Alt, "F4");

        var saved = repository.Load();
        Assert.Single(saved.CustomButtons);
        Assert.Equal("Alt + F4", service.DescribeMapping(ExtraId));
    }

    [Fact]
    public void AHidButtonThatXInputNeverReportsIsDetectedAsAnExtraButton()
    {
        var service = Create(out _);

        _rawSource.Enqueue(new RawGamepadButtonPress("045E-0B12", 15, _now));
        service.Process(None(), executeActions: false);

        Assert.Empty(service.DetectedButtons);

        Advance(TimeSpan.FromMilliseconds(100));
        service.Process(None(), executeActions: false);

        var button = Assert.Single(service.DetectedButtons);
        Assert.Equal(ButtonIds.ForHid("045E-0B12", 15), button.Id);
        Assert.Equal("HID 15", button.Label);
        Assert.True(button.Confirmed);
    }

    [Fact]
    public void AHidReportThatEchoesAnXInputPressIsNotTreatedAsAnExtraButton()
    {
        var service = Create(out _);

        service.NoteControllerActivity(Press(GamepadButton.A));
        _rawSource.Enqueue(new RawGamepadButtonPress("045E-0B12", 1, _now));

        Advance(TimeSpan.FromMilliseconds(100));
        service.Process(None(), executeActions: false);

        Assert.Empty(service.DetectedButtons);
    }

    [Fact]
    public void AMappedHidButtonFiresItsCombination()
    {
        var service = Create(out _);
        var hidId = ButtonIds.ForHid("045E-0B12", 15);

        _rawSource.Enqueue(new RawGamepadButtonPress("045E-0B12", 15, _now));
        Advance(TimeSpan.FromMilliseconds(100));
        service.Process(None(), executeActions: false);

        service.Assign(hidId, KeyModifiers.Alt, "Tab");

        _rawSource.Enqueue(new RawGamepadButtonPress("045E-0B12", 15, _now));
        service.Process(None(), executeActions: true);

        var combination = Assert.Single(_keyboard.Combinations);
        Assert.Equal(KeyModifiers.Alt, combination.Modifiers);
    }

    [Fact]
    public void RawDetectionIsReportedAsUnavailableWhenTheWatcherIsNotRunning()
    {
        _rawSource.IsRunning = false;
        var service = Create(out _);

        Assert.False(service.RawDetectionAvailable);

        _rawSource.IsRunning = true;
        Assert.True(service.RawDetectionAvailable);
    }

    [Fact]
    public void EveryCatalogKeyDescribesCleanlyWithModifiers()
    {
        foreach (var key in KeyCatalog.All)
        {
            var description = KeyModifiersFormatting.Describe(KeyModifiers.Control | KeyModifiers.Alt, key.Name);
            Assert.Equal($"Ctrl + Alt + {key.Name}", description);
        }
    }

    private CustomButtonService Create(out SettingsRepository repository)
    {
        repository = new SettingsRepository(_settingsPath);
        return new CustomButtonService(AppSettings.CreateDefault(), repository, _keyboard, _rawSource, () => _now);
    }

    private CustomButtonService CreateConfirmed(GamepadButton button, out SettingsRepository repository)
    {
        var service = Create(out repository);
        service.Process(Press(button), executeActions: false);
        return service;
    }

    private void Advance(TimeSpan amount) => _now += amount;

    private static ButtonTransitions Press(GamepadButton button) => new(button, GamepadButton.None);

    private static ButtonTransitions None() => new(GamepadButton.None, GamepadButton.None);

}
