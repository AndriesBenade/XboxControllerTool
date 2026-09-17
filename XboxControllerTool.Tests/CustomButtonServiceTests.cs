using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Simulation;

namespace XboxControllerTool.Tests;

public class CustomButtonServiceTests : IDisposable
{
    private const ushort RightThumb = (ushort)GamepadButton.RightThumb;
    private const ushort Guide = (ushort)GamepadButton.Guide;

    private readonly string _directory;
    private readonly string _settingsPath;
    private readonly RecordingKeyboard _keyboard = new();

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
    }

    [Fact]
    public void LeftStickClickIsNotOfferedForRemappingBecauseItIsDeliberatelyUnassigned()
    {
        Assert.False(CustomButtonService.IsMappable((ushort)GamepadButton.LeftThumb));
    }

    [Fact]
    public void TheRightStickClickIsAvailableWithoutHavingToBePressedFirst()
    {
        var service = Create(out _);

        Assert.Contains(service.DetectedButtons, button => button.Mask == RightThumb);
    }

    [Fact]
    public void PressingASpareButtonAddsItToTheDetectedList()
    {
        var service = Create(out _);

        Assert.DoesNotContain(service.DetectedButtons, button => button.Mask == Guide);

        service.Process(Press(GamepadButton.Guide), executeActions: true);

        Assert.Contains(service.DetectedButtons, button => button.Mask == Guide);
    }

    [Fact]
    public void PressingABuiltInButtonNeverAddsItToTheDetectedList()
    {
        var service = Create(out _);

        service.Process(Press(GamepadButton.A), executeActions: true);
        service.Process(Press(GamepadButton.LeftThumb), executeActions: true);

        Assert.DoesNotContain(service.DetectedButtons, button => button.Mask == (ushort)GamepadButton.A);
        Assert.DoesNotContain(service.DetectedButtons, button => button.Mask == (ushort)GamepadButton.LeftThumb);
    }

    [Fact]
    public void AMappedButtonSendsItsCombinationOncePerPress()
    {
        var service = Create(out _);
        service.Assign(RightThumb, KeyModifiers.Windows, "D");

        service.Process(Press(GamepadButton.RightThumb), executeActions: true);
        service.Process(new ButtonTransitions(GamepadButton.None, GamepadButton.None), executeActions: true);
        service.Process(new ButtonTransitions(GamepadButton.None, GamepadButton.RightThumb), executeActions: true);

        var combination = Assert.Single(_keyboard.Combinations);
        Assert.Equal(KeyModifiers.Windows, combination.Modifiers);
        Assert.Equal(0x44, combination.VirtualKey);
    }

    [Fact]
    public void HoldingAMappedButtonDoesNotRepeatTheCombination()
    {
        var service = Create(out _);
        service.Assign(RightThumb, KeyModifiers.Alt, "Tab");

        service.Process(Press(GamepadButton.RightThumb), executeActions: true);

        for (var i = 0; i < 100; i++)
        {
            service.Process(new ButtonTransitions(GamepadButton.None, GamepadButton.None), executeActions: true);
        }

        Assert.Single(_keyboard.Combinations);
    }

    [Fact]
    public void AnUnmappedButtonPressSendsNothing()
    {
        var service = Create(out _);

        service.Process(Press(GamepadButton.RightThumb), executeActions: true);

        Assert.Empty(_keyboard.Combinations);
    }

    [Fact]
    public void ButtonsAreStillDetectedWhileActionsAreSuppressed()
    {
        var service = Create(out _);
        service.Assign(RightThumb, KeyModifiers.Windows, "D");

        service.Process(Press(GamepadButton.Guide), executeActions: false);
        service.Process(Press(GamepadButton.RightThumb), executeActions: false);

        Assert.Contains(service.DetectedButtons, button => button.Mask == Guide);
        Assert.Empty(_keyboard.Combinations);
    }

    [Fact]
    public void MappingsSurviveARestart()
    {
        var service = Create(out var repository);
        service.Assign(RightThumb, KeyModifiers.Control | KeyModifiers.Shift, "Esc");

        var reloaded = new CustomButtonService(repository.Load(), repository, _keyboard);

        Assert.Equal("Ctrl + Shift + Esc", reloaded.DescribeMapping(RightThumb));
    }

    [Fact]
    public void ClearingAMappingRemovesItAndStopsItFiring()
    {
        var service = Create(out _);
        service.Assign(RightThumb, KeyModifiers.Windows, "D");

        service.Clear(RightThumb);
        service.Process(Press(GamepadButton.RightThumb), executeActions: true);

        Assert.Equal("Unassigned", service.DescribeMapping(RightThumb));
        Assert.Empty(_keyboard.Combinations);
    }

    [Fact]
    public void MappingsForUnknownKeysOrReservedButtonsAreDiscardedOnLoad()
    {
        var settings = AppSettings.CreateDefault();
        settings.CustomButtons.Add(new CustomButtonMapping { Button = RightThumb, Key = "NotARealKey" });
        settings.CustomButtons.Add(new CustomButtonMapping { Button = (ushort)GamepadButton.A, Key = "D" });
        settings.CustomButtons.Add(new CustomButtonMapping { Button = Guide, Key = "F5" });

        var repository = new SettingsRepository(_settingsPath);
        var service = new CustomButtonService(settings, repository, _keyboard);

        Assert.Equal("Unassigned", service.DescribeMapping(RightThumb));
        Assert.Equal("Unassigned", service.DescribeMapping((ushort)GamepadButton.A));
        Assert.Equal("F5", service.DescribeMapping(Guide));
    }

    [Fact]
    public void AssigningIsRejectedForReservedButtonsAndUnknownKeys()
    {
        var service = Create(out _);

        service.Assign((ushort)GamepadButton.A, KeyModifiers.None, "D");
        service.Assign((ushort)GamepadButton.LeftThumb, KeyModifiers.None, "D");
        service.Assign(RightThumb, KeyModifiers.None, "NotARealKey");

        Assert.Equal("Unassigned", service.DescribeMapping((ushort)GamepadButton.A));
        Assert.Equal("Unassigned", service.DescribeMapping((ushort)GamepadButton.LeftThumb));
        Assert.Equal("Unassigned", service.DescribeMapping(RightThumb));
    }

    [Fact]
    public void ReassigningAButtonReplacesTheExistingMappingRatherThanAddingASecond()
    {
        var service = Create(out var repository);

        service.Assign(RightThumb, KeyModifiers.Windows, "D");
        service.Assign(RightThumb, KeyModifiers.Alt, "F4");

        var saved = repository.Load();
        Assert.Single(saved.CustomButtons);
        Assert.Equal("Alt + F4", service.DescribeMapping(RightThumb));
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
        return new CustomButtonService(AppSettings.CreateDefault(), repository, _keyboard);
    }

    private static ButtonTransitions Press(GamepadButton button) => new(button, GamepadButton.None);
}
