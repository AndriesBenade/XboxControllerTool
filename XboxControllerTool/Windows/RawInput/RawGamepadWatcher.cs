using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace XboxControllerTool.Windows.RawInput;

/// <summary>
/// Watches raw HID gamepad reports so buttons that XInput cannot report - paddles and extra
/// buttons such as M1, M2, MX, AGL and AGR - can still be seen. XInput only ever exposes a fixed
/// sixteen bit button mask, so raw HID is the only route to anything beyond it. Everything here is
/// best effort: if registration or HID parsing fails the watcher simply reports nothing.
/// </summary>
public sealed class RawGamepadWatcher : IRawGamepadSource, IDisposable
{
    private readonly ConcurrentQueue<RawGamepadButtonPress> _presses = new();
    private readonly Dictionary<nint, HashSet<ushort>> _heldByDevice = [];
    private readonly Dictionary<nint, string> _deviceIds = [];
    private readonly ManualResetEventSlim _ready = new(false);

    private Thread? _thread;
    private nint _windowHandle;
    private nint _preparsedBuffer;
    private int _preparsedCapacity;
    private nint _reportBuffer;
    private int _reportCapacity;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (_thread is not null)
        {
            return;
        }

        _thread = new Thread(RunMessageLoop) { IsBackground = true, Name = "RawGamepadWatcher" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait(TimeSpan.FromSeconds(5));
    }

    public bool TryDequeue(out RawGamepadButtonPress press) => _presses.TryDequeue(out press);

    public IReadOnlyList<RawGamepadDevice> DescribeDevices() => RawGamepadInventory.Describe();

    private void RunMessageLoop()
    {
        MessageWindow? window = null;

        try
        {
            window = new MessageWindow(HandleRawInput);
            _windowHandle = window.Handle;
            IsRunning = Register(_windowHandle);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException)
        {
            IsRunning = false;
        }
        finally
        {
            _ready.Set();
        }

        if (IsRunning)
        {
            System.Windows.Forms.Application.Run();
        }
        else
        {
            window?.Close();
        }
    }

    private static bool Register(nint target)
    {
        RawInputDevice[] devices =
        [
            Describe(RawInputNative.UsageGamepad, target),
            Describe(RawInputNative.UsageJoystick, target),
            Describe(RawInputNative.UsageMultiAxisController, target)
        ];

        return RawInputNative.RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>());
    }

    private static RawInputDevice Describe(ushort usage, nint target) => new()
    {
        UsagePage = RawInputNative.UsagePageGeneric,
        Usage = usage,
        Flags = RawInputNative.RawInputSink,
        Target = target
    };

    private void HandleRawInput(nint rawInputHandle)
    {
        try
        {
            ProcessRawInput(rawInputHandle);
        }
        catch (Exception ex) when (ex is SEHException or ArgumentException or OutOfMemoryException)
        {
            // A device reporting something unexpected must never take the whole application down.
        }
    }

    private void ProcessRawInput(nint rawInputHandle)
    {
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        uint size = 0;

        if (RawInputNative.GetRawInputData(rawInputHandle, RawInputNative.RidInput, nint.Zero, ref size, headerSize) != 0 || size == 0)
        {
            return;
        }

        EnsureReportCapacity((int)size);

        if (RawInputNative.GetRawInputData(rawInputHandle, RawInputNative.RidInput, _reportBuffer, ref size, headerSize) != size)
        {
            return;
        }

        var header = Marshal.PtrToStructure<RawInputHeader>(_reportBuffer);

        if (header.Type != RawInputNative.RimTypeHid || header.Device == nint.Zero)
        {
            return;
        }

        var hidOffset = (int)headerSize;
        var reportSize = Marshal.ReadInt32(_reportBuffer, hidOffset);
        var reportCount = Marshal.ReadInt32(_reportBuffer, hidOffset + sizeof(int));

        if (reportSize <= 0 || reportCount <= 0)
        {
            return;
        }

        var preparsed = GetPreparsedData(header.Device);

        if (preparsed == nint.Zero)
        {
            return;
        }

        var maxUsages = RawInputNative.HidP_MaxUsageListLength(RawInputNative.HidPInput, RawInputNative.UsagePageButton, preparsed);

        if (maxUsages <= 0)
        {
            return;
        }

        var reportStart = _reportBuffer + hidOffset + (2 * sizeof(int));
        var usages = new ushort[maxUsages];
        var pressed = new HashSet<ushort>();

        for (var index = 0; index < reportCount; index++)
        {
            var usageLength = maxUsages;

            var status = RawInputNative.HidP_GetUsages(
                RawInputNative.HidPInput, RawInputNative.UsagePageButton, 0,
                usages, ref usageLength, preparsed, reportStart + (index * reportSize), reportSize);

            if (status != RawInputNative.HidPStatusSuccess)
            {
                continue;
            }

            for (var usageIndex = 0; usageIndex < usageLength; usageIndex++)
            {
                pressed.Add(usages[usageIndex]);
            }
        }

        RecordTransitions(header.Device, pressed);
    }

    private void RecordTransitions(nint device, HashSet<ushort> pressed)
    {
        if (!_heldByDevice.TryGetValue(device, out var previouslyHeld))
        {
            previouslyHeld = [];
            _heldByDevice[device] = previouslyHeld;
        }

        var deviceId = GetDeviceId(device);
        var now = DateTime.UtcNow;

        foreach (var usage in pressed)
        {
            if (previouslyHeld.Add(usage))
            {
                _presses.Enqueue(new RawGamepadButtonPress(deviceId, usage, now));
            }
        }

        previouslyHeld.RemoveWhere(usage => !pressed.Contains(usage));
    }

    private string GetDeviceId(nint device)
    {
        if (_deviceIds.TryGetValue(device, out var cached))
        {
            return cached;
        }

        var id = ReadDeviceIdentity(device);
        _deviceIds[device] = id;
        return id;
    }

    private static string ReadDeviceIdentity(nint device)
    {
        uint size = 0;

        if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiDeviceName, nint.Zero, ref size) != 0 || size == 0)
        {
            return "UNKNOWN";
        }

        var buffer = Marshal.AllocHGlobal((int)size * sizeof(char));

        try
        {
            if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiDeviceName, buffer, ref size) == uint.MaxValue)
            {
                return "UNKNOWN";
            }

            return RawGamepadInventory.IdentityOf(Marshal.PtrToStringUni(buffer) ?? string.Empty);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private nint GetPreparsedData(nint device)
    {
        uint size = 0;

        if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiPreparsedData, nint.Zero, ref size) != 0 || size == 0)
        {
            return nint.Zero;
        }

        EnsurePreparsedCapacity((int)size);

        return RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiPreparsedData, _preparsedBuffer, ref size) == uint.MaxValue
            ? nint.Zero
            : _preparsedBuffer;
    }

    private void EnsureReportCapacity(int size)
    {
        if (size <= _reportCapacity)
        {
            return;
        }

        if (_reportBuffer != nint.Zero)
        {
            Marshal.FreeHGlobal(_reportBuffer);
        }

        _reportBuffer = Marshal.AllocHGlobal(size);
        _reportCapacity = size;
    }

    private void EnsurePreparsedCapacity(int size)
    {
        if (size <= _preparsedCapacity)
        {
            return;
        }

        if (_preparsedBuffer != nint.Zero)
        {
            Marshal.FreeHGlobal(_preparsedBuffer);
        }

        _preparsedBuffer = Marshal.AllocHGlobal(size);
        _preparsedCapacity = size;
    }

    public void Dispose()
    {
        if (_windowHandle != nint.Zero)
        {
            RawInputNative.PostMessage(_windowHandle, RawInputNative.WmClose, nint.Zero, nint.Zero);
            _windowHandle = nint.Zero;
        }

        _thread?.Join(TimeSpan.FromSeconds(1));
        _thread = null;
        IsRunning = false;

        if (_reportBuffer != nint.Zero)
        {
            Marshal.FreeHGlobal(_reportBuffer);
            _reportBuffer = nint.Zero;
            _reportCapacity = 0;
        }

        if (_preparsedBuffer != nint.Zero)
        {
            Marshal.FreeHGlobal(_preparsedBuffer);
            _preparsedBuffer = nint.Zero;
            _preparsedCapacity = 0;
        }

        _ready.Dispose();
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public int Type;
        public int Size;
        public nint Device;
        public nint WParam;
    }

    private sealed class MessageWindow : NativeWindow
    {
        private readonly Action<nint> _onRawInput;

        public MessageWindow(Action<nint> onRawInput)
        {
            _onRawInput = onRawInput;
            CreateHandle(new CreateParams { Caption = "XboxControllerToolRawInput" });
        }

        public void Close()
        {
            if (Handle != nint.Zero)
            {
                DestroyHandle();
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case RawInputNative.WmInput:
                    _onRawInput(m.LParam);
                    break;
                case RawInputNative.WmClose:
                    DestroyHandle();
                    System.Windows.Forms.Application.ExitThread();
                    return;
            }

            base.WndProc(ref m);
        }
    }
}
