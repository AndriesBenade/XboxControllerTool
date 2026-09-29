using System.Runtime.InteropServices;
using XboxControllerTool.ConsoleUi;

namespace XboxControllerTool.Windows;

public enum ConsoleInputKind
{
    Key,
    Click,
    Wheel
}

public readonly record struct ConsoleInputEvent(ConsoleInputKind Kind, MenuAction Action, int Row);

/// <summary>
/// Lets the keyboard and mouse drive the menu, so the app is usable at the desk as well as from the
/// couch.
/// <para>
/// Mouse events only reach a console application once quick-edit mode is off, because quick edit
/// claims clicks for text selection. That is the trade: click-dragging to select console text stops
/// working while the app runs, and the original mode is put back on exit.
/// </para>
/// </summary>
public sealed class ConsoleInputSource : IDisposable
{
    private const int StdInputHandle = -10;

    private const uint EnableProcessedInput = 0x0001;
    private const uint EnableLineInput = 0x0002;
    private const uint EnableEchoInput = 0x0004;
    private const uint EnableWindowInput = 0x0008;
    private const uint EnableMouseInput = 0x0010;
    private const uint EnableQuickEditMode = 0x0040;
    private const uint EnableExtendedFlags = 0x0080;
    private const uint EnableVirtualTerminalInput = 0x0200;

    private const ushort KeyEventType = 0x0001;
    private const ushort MouseEventType = 0x0002;

    private const uint LeftButtonPressed = 0x0001;
    private const uint MouseMoved = 0x0001;
    private const uint MouseWheeled = 0x0004;
    private const uint DoubleClick = 0x0002;

    private const ushort VkBack = 0x08;
    private const ushort VkReturn = 0x0D;
    private const ushort VkEscape = 0x1B;
    private const ushort VkSpace = 0x20;
    private const ushort VkLeft = 0x25;
    private const ushort VkUp = 0x26;
    private const ushort VkRight = 0x27;
    private const ushort VkDown = 0x28;
    private const ushort VkDelete = 0x2E;
    private const ushort VkX = 0x58;

    private const int MaxEventsPerDrain = 32;

    private readonly nint _handle;
    private readonly uint _originalMode;
    private readonly bool _ready;

    private uint _previousButtons;

    public ConsoleInputSource()
    {
        _handle = GetStdHandle(StdInputHandle);

        if (_handle == nint.Zero || _handle == -1 || !GetConsoleMode(_handle, out _originalMode))
        {
            return;
        }

        // Extended flags must be set for the quick-edit bit to be honoured at all.
        var mode = _originalMode;
        mode |= EnableExtendedFlags | EnableMouseInput | EnableWindowInput | EnableProcessedInput;
        mode &= ~(EnableQuickEditMode | EnableLineInput | EnableEchoInput | EnableVirtualTerminalInput);

        _ready = SetConsoleMode(_handle, mode);
    }

    public bool IsAvailable => _ready;

    /// <summary>
    /// Takes whatever has arrived without ever waiting. The input loop runs every few milliseconds
    /// and must not block on someone not touching the keyboard.
    /// </summary>
    public IReadOnlyList<ConsoleInputEvent> Drain()
    {
        if (!_ready || !GetNumberOfConsoleInputEvents(_handle, out var pending) || pending == 0)
        {
            return [];
        }

        var buffer = new InputRecord[Math.Min(pending, MaxEventsPerDrain)];

        if (!ReadConsoleInput(_handle, buffer, (uint)buffer.Length, out var read) || read == 0)
        {
            return [];
        }

        var events = new List<ConsoleInputEvent>();

        for (var index = 0; index < read; index++)
        {
            if (Translate(buffer[index]) is { } translated)
            {
                events.Add(translated);
            }
        }

        return events;
    }

    private ConsoleInputEvent? Translate(InputRecord record) => record.EventType switch
    {
        KeyEventType => TranslateKey(record.Key),
        MouseEventType => TranslateMouse(record.Mouse),
        _ => null
    };

    private static ConsoleInputEvent? TranslateKey(KeyEventRecord key)
    {
        if (!key.KeyDown)
        {
            return null;
        }

        var action = key.VirtualKeyCode switch
        {
            VkUp => MenuAction.Up,
            VkDown => MenuAction.Down,
            VkLeft => MenuAction.Left,
            VkRight => MenuAction.Right,
            VkReturn or VkSpace => MenuAction.Confirm,
            VkEscape or VkBack => MenuAction.Cancel,
            VkDelete or VkX => MenuAction.Clear,
            _ => MenuAction.None
        };

        return action == MenuAction.None ? null : new ConsoleInputEvent(ConsoleInputKind.Key, action, -1);
    }

    private ConsoleInputEvent? TranslateMouse(MouseEventRecord mouse)
    {
        if ((mouse.EventFlags & MouseWheeled) != 0)
        {
            // The scroll amount lives in the high word, signed: away from the user is positive.
            var delta = (short)(mouse.ButtonState >> 16);
            return delta == 0
                ? null
                : new ConsoleInputEvent(ConsoleInputKind.Wheel, delta > 0 ? MenuAction.Up : MenuAction.Down, -1);
        }

        var wasPressed = (_previousButtons & LeftButtonPressed) != 0;
        var isPressed = (mouse.ButtonState & LeftButtonPressed) != 0;
        _previousButtons = mouse.ButtonState;

        // Only the moment the button goes down counts, so holding it does not repeat, and a move
        // with the button already held is a drag rather than a new click.
        if (!isPressed || wasPressed || (mouse.EventFlags & MouseMoved) != 0)
        {
            return null;
        }

        if ((mouse.EventFlags & ~DoubleClick) != 0)
        {
            return null;
        }

        return new ConsoleInputEvent(ConsoleInputKind.Click, MenuAction.Confirm, mouse.MousePosition.Y);
    }

    public void Dispose()
    {
        if (_ready)
        {
            SetConsoleMode(_handle, _originalMode);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Coord
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyEventRecord
    {
        [MarshalAs(UnmanagedType.Bool)] public bool KeyDown;
        public ushort RepeatCount;
        public ushort VirtualKeyCode;
        public ushort VirtualScanCode;
        public char Character;
        public uint ControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseEventRecord
    {
        public Coord MousePosition;
        public uint ButtonState;
        public uint ControlKeyState;
        public uint EventFlags;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputRecord
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public KeyEventRecord Key;
        [FieldOffset(4)] public MouseEventRecord Mouse;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(nint handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(nint handle, uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNumberOfConsoleInputEvents(nint handle, out uint count);

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadConsoleInputW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadConsoleInput(nint handle, [Out] InputRecord[] buffer, uint length, out uint read);
}
