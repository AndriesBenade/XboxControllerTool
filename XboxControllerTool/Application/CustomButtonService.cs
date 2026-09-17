using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Application;

public readonly record struct ExtraButton(string Id, string Label, bool Confirmed);

public sealed class CustomButtonService
{
    // Every button the built-in mappings already own, plus the left stick click, which is
    // deliberately left with no action at all and therefore must not be offered here either.
    private const ushort ReservedButtons =
        (ushort)(GamepadButton.A | GamepadButton.B | GamepadButton.X | GamepadButton.Y |
                 GamepadButton.LeftShoulder | GamepadButton.RightShoulder |
                 GamepadButton.Start | GamepadButton.Back |
                 GamepadButton.DPadUp | GamepadButton.DPadDown | GamepadButton.DPadLeft | GamepadButton.DPadRight |
                 GamepadButton.LeftThumb | GamepadButton.RightThumb);

    private readonly AppSettings _settings;
    private readonly SettingsRepository _repository;
    private readonly IKeyboardInput _keyboard;
    private readonly IRawGamepadSource? _rawSource;
    private readonly RawButtonCorrelator _correlator = new();
    private readonly Func<DateTime> _clock;
    private readonly Dictionary<string, bool> _detected = [];

    public CustomButtonService(
        AppSettings settings,
        SettingsRepository repository,
        IKeyboardInput keyboard,
        IRawGamepadSource? rawSource = null,
        Func<DateTime>? clock = null)
    {
        _settings = settings;
        _repository = repository;
        _keyboard = keyboard;
        _rawSource = rawSource;
        _clock = clock ?? (() => DateTime.UtcNow);

        NormalizeMappings();

        // A saved mapping keeps its button on the list so it can be reviewed or cleared, but the
        // button counts as unconfirmed until it is pressed again in this session.
        foreach (var mapping in _settings.CustomButtons)
        {
            _detected.TryAdd(mapping.ButtonId, false);
        }
    }

    /// <summary>
    /// True when raw HID monitoring started, which is what allows buttons XInput cannot report to
    /// be discovered at all.
    /// </summary>
    public bool RawDetectionAvailable => _rawSource?.IsRunning ?? false;

    /// <summary>
    /// The attached gamepads and the number of buttons each one declares to Windows. A button the
    /// controller never declares cannot be detected by any application, so this is what explains an
    /// extra button that refuses to show up.
    /// </summary>
    public IReadOnlyList<RawGamepadDevice> Devices => _rawSource?.DescribeDevices() ?? [];

    /// <summary>Increments on every press of a mappable button, so the UI can react to one.</summary>
    public int DetectionSequence { get; private set; }

    public ExtraButton? LastDetected { get; private set; }

    public IReadOnlyList<ExtraButton> DetectedButtons =>
    [
        .. _detected
            .OrderBy(entry => ButtonIds.IsHid(entry.Key))
            .ThenBy(entry => SortKey(entry.Key))
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new ExtraButton(entry.Key, ButtonIds.LabelFor(entry.Key), entry.Value))
    ];

    public static bool IsMappable(ushort mask) => mask != 0 && (mask & ReservedButtons) == 0;

    public static bool IsMappable(string buttonId) =>
        ButtonIds.TryParseXInput(buttonId, out var mask) ? IsMappable(mask) : ButtonIds.TryParseHidUsage(buttonId, out _);

    public static string LabelFor(string buttonId) => ButtonIds.LabelFor(buttonId);

    public bool IsConfirmed(string buttonId) => _detected.TryGetValue(buttonId, out var confirmed) && confirmed;

    public CustomButtonMapping? FindMapping(string buttonId) =>
        _settings.CustomButtons.FirstOrDefault(mapping => mapping.ButtonId == buttonId);

    public string DescribeMapping(string buttonId)
    {
        var mapping = FindMapping(buttonId);

        return mapping is null
            ? "Unassigned"
            : KeyModifiersFormatting.Describe(mapping.Modifiers, mapping.Key);
    }

    /// <summary>
    /// Stores a key combination for a button. A button that has not been pressed and detected in
    /// this session is rejected, so nothing can be mapped to a button this machine cannot see.
    /// </summary>
    public void Assign(string buttonId, KeyModifiers modifiers, string keyName)
    {
        if (!IsMappable(buttonId) || !IsConfirmed(buttonId) || !KeyCatalog.IsKnown(keyName))
        {
            return;
        }

        var mapping = FindMapping(buttonId);

        if (mapping is null)
        {
            mapping = new CustomButtonMapping { ButtonId = buttonId };
            _settings.CustomButtons.Add(mapping);
        }

        mapping.Modifiers = modifiers;
        mapping.Key = keyName;
        _repository.Save(_settings);
    }

    public void Clear(string buttonId)
    {
        var mapping = FindMapping(buttonId);

        if (mapping is null)
        {
            return;
        }

        _settings.CustomButtons.Remove(mapping);
        _repository.Save(_settings);
    }

    /// <summary>
    /// Records controller activity for correlation only. Presses on every slot are reported here,
    /// including slots the user excluded, so a HID report can be recognised as the echo of an
    /// ordinary XInput button rather than mistaken for an extra one.
    /// </summary>
    public void NoteControllerActivity(ButtonTransitions transitions)
    {
        if (transitions.Pressed != GamepadButton.None)
        {
            _correlator.NoteControllerPress(_clock());
        }
    }

    /// <summary>
    /// Detects mappable button presses and, when <paramref name="executeActions"/> is set, fires
    /// the mapped key combination once per press. This runs regardless of the game-focus pause so
    /// that Windows-level shortcuts stay available while a game owns the controller.
    /// </summary>
    public void Process(ButtonTransitions transitions, bool executeActions)
    {
        var now = _clock();
        var pressed = (ushort)transitions.Pressed;

        if (pressed != 0)
        {
            _correlator.NoteControllerPress(now);

            for (var bit = 1; bit <= 0x8000; bit <<= 1)
            {
                var mask = (ushort)bit;

                if ((pressed & mask) != 0 && IsMappable(mask))
                {
                    Handle(ButtonIds.ForXInput(mask), executeActions);
                }
            }
        }

        DrainRawPresses(executeActions);

        foreach (var press in _correlator.Release(now))
        {
            Handle(ButtonIds.ForHid(press.DeviceId, press.Usage), executeActions);
        }
    }

    public void ReleaseModifiers() => _keyboard.ReleaseModifiers();

    private void DrainRawPresses(bool executeActions)
    {
        if (_rawSource is null)
        {
            return;
        }

        while (_rawSource.TryDequeue(out var press))
        {
            var buttonId = ButtonIds.ForHid(press.DeviceId, press.Usage);

            // A button already known to be invisible to XInput never needs correlating again, so
            // it responds immediately instead of waiting out the correlation window.
            if (_detected.ContainsKey(buttonId))
            {
                Handle(buttonId, executeActions);
            }
            else
            {
                _correlator.NoteRawPress(press);
            }
        }
    }

    private void Handle(string buttonId, bool executeActions)
    {
        if (!IsMappable(buttonId))
        {
            return;
        }

        _detected[buttonId] = true;
        DetectionSequence++;
        LastDetected = new ExtraButton(buttonId, ButtonIds.LabelFor(buttonId), true);

        if (!executeActions)
        {
            return;
        }

        if (FindMapping(buttonId) is { } mapping && KeyCatalog.Find(mapping.Key) is { } key)
        {
            _keyboard.SendCombination(mapping.Modifiers, key.VirtualKey);
        }
    }

    private static int SortKey(string buttonId)
    {
        if (ButtonIds.TryParseXInput(buttonId, out var mask))
        {
            return mask;
        }

        return ButtonIds.TryParseHidUsage(buttonId, out var usage) ? usage : int.MaxValue;
    }

    private void NormalizeMappings()
    {
        var changed = false;

        foreach (var mapping in _settings.CustomButtons)
        {
            if (mapping.Button != 0)
            {
                if (string.IsNullOrEmpty(mapping.ButtonId))
                {
                    mapping.ButtonId = ButtonIds.ForXInput(mapping.Button);
                }

                mapping.Button = 0;
                changed = true;
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        var removed = _settings.CustomButtons.RemoveAll(mapping =>
            !IsMappable(mapping.ButtonId) || !KeyCatalog.IsKnown(mapping.Key) || !seen.Add(mapping.ButtonId));

        if (changed || removed > 0)
        {
            _repository.Save(_settings);
        }
    }
}
