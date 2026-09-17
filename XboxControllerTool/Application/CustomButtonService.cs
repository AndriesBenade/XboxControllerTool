using XboxControllerTool.Configuration;
using XboxControllerTool.Core;
using XboxControllerTool.Simulation;

namespace XboxControllerTool.Application;

public readonly record struct ExtraButton(ushort Mask, string Label);

public sealed class CustomButtonService
{
    // Every button the built-in mappings already own, plus the left stick click, which is
    // deliberately left with no action at all and therefore must not be offered here either.
    private const ushort ReservedButtons =
        (ushort)(GamepadButton.A | GamepadButton.B | GamepadButton.X | GamepadButton.Y |
                 GamepadButton.LeftShoulder | GamepadButton.RightShoulder |
                 GamepadButton.Start | GamepadButton.Back |
                 GamepadButton.DPadUp | GamepadButton.DPadDown | GamepadButton.DPadLeft | GamepadButton.DPadRight |
                 GamepadButton.LeftThumb);

    private static readonly IReadOnlyDictionary<ushort, string> KnownLabels = new Dictionary<ushort, string>
    {
        [(ushort)GamepadButton.RightThumb] = "R3",
        [(ushort)GamepadButton.Guide] = "GUIDE",
        [(ushort)GamepadButton.Extra] = "EXTRA"
    };

    private readonly AppSettings _settings;
    private readonly SettingsRepository _repository;
    private readonly IKeyboardInput _keyboard;
    private readonly HashSet<ushort> _detected = [];

    public CustomButtonService(AppSettings settings, SettingsRepository repository, IKeyboardInput keyboard)
    {
        _settings = settings;
        _repository = repository;
        _keyboard = keyboard;

        // The right stick click exists on every XInput controller and has no built-in action,
        // so it is always offered. Anything else has to actually be seen before it is listed.
        _detected.Add((ushort)GamepadButton.RightThumb);

        RemoveInvalidMappings();

        foreach (var mapping in _settings.CustomButtons)
        {
            _detected.Add(mapping.Button);
        }
    }

    public IReadOnlyList<ExtraButton> DetectedButtons =>
        [.. _detected.OrderBy(mask => mask).Select(mask => new ExtraButton(mask, LabelFor(mask)))];

    public static bool IsMappable(ushort mask) => mask != 0 && (mask & ReservedButtons) == 0;

    public static string LabelFor(ushort mask) =>
        KnownLabels.TryGetValue(mask, out var label) ? label : $"BUTTON {mask:X3}";

    public CustomButtonMapping? FindMapping(ushort mask) =>
        _settings.CustomButtons.FirstOrDefault(mapping => mapping.Button == mask);

    public string DescribeMapping(ushort mask)
    {
        var mapping = FindMapping(mask);

        return mapping is null
            ? "Unassigned"
            : KeyModifiersFormatting.Describe(mapping.Modifiers, mapping.Key);
    }

    public void Assign(ushort mask, KeyModifiers modifiers, string keyName)
    {
        if (!IsMappable(mask) || !KeyCatalog.IsKnown(keyName))
        {
            return;
        }

        var mapping = FindMapping(mask);

        if (mapping is null)
        {
            mapping = new CustomButtonMapping { Button = mask };
            _settings.CustomButtons.Add(mapping);
        }

        mapping.Modifiers = modifiers;
        mapping.Key = keyName;
        _repository.Save(_settings);
    }

    public void Clear(ushort mask)
    {
        var mapping = FindMapping(mask);

        if (mapping is null)
        {
            return;
        }

        _settings.CustomButtons.Remove(mapping);
        _repository.Save(_settings);
    }

    /// <summary>
    /// Records any mappable button that is pressed and, when <paramref name="executeActions"/> is set,
    /// fires its key combination once per press. This runs regardless of the game-focus pause so that
    /// Windows-level shortcuts stay available while a game owns the controller.
    /// </summary>
    public void Process(ButtonTransitions transitions, bool executeActions)
    {
        var pressed = (ushort)transitions.Pressed;

        if (pressed == 0)
        {
            return;
        }

        for (var bit = 1; bit <= 0x8000; bit <<= 1)
        {
            var mask = (ushort)bit;

            if ((pressed & mask) == 0 || !IsMappable(mask))
            {
                continue;
            }

            _detected.Add(mask);

            if (!executeActions)
            {
                continue;
            }

            var mapping = FindMapping(mask);

            if (mapping is null)
            {
                continue;
            }

            if (KeyCatalog.Find(mapping.Key) is { } key)
            {
                _keyboard.SendCombination(mapping.Modifiers, key.VirtualKey);
            }
        }
    }

    public void ReleaseModifiers() => _keyboard.ReleaseModifiers();

    private void RemoveInvalidMappings()
    {
        var removed = _settings.CustomButtons.RemoveAll(mapping =>
            !IsMappable(mapping.Button) || !KeyCatalog.IsKnown(mapping.Key));

        if (removed > 0)
        {
            _repository.Save(_settings);
        }
    }
}
