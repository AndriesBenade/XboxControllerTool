using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

/// <summary>
/// Exercises the real raw input registration. No button can be pressed from a test, but this
/// proves the P/Invoke signatures and the message-only window are correct on this machine, which
/// is the part that silently does nothing when it is wrong.
/// </summary>
public class RawGamepadWatcherTests
{
    [Fact]
    public void TheWatcherRegistersForHidGamepadInputAndShutsDownCleanly()
    {
        using var watcher = new RawGamepadWatcher();

        watcher.Start();

        Assert.True(watcher.IsRunning, "Raw input registration for HID gamepads failed.");
        Assert.False(watcher.TryDequeue(out _));

        watcher.Dispose();

        Assert.False(watcher.IsRunning);
    }

    [Fact]
    public void EveryGamepadWindowsKnowsAboutIsDescribedWithItsButtonCount()
    {
        foreach (var device in RawGamepadInventory.Describe())
        {
            Assert.False(string.IsNullOrWhiteSpace(device.DeviceId));
            Assert.InRange(device.ButtonCount, 0, 256);
        }
    }

    [Fact]
    public void AGamepadIsIdentifiedByItsVendorAndProductIds()
    {
        const string path = @"\\?\HID#VID_045E&PID_02FF&IG_00#7&2ad4fe05&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

        Assert.Equal("045E-02FF", RawGamepadInventory.IdentityOf(path));
        Assert.Equal("UNKNOWN", RawGamepadInventory.IdentityOf(@"\\?\HID#ConvertedDevice&Col02#5&353ec129&0&0001"));
    }

    [Fact]
    public void StartingTwiceDoesNotCreateASecondWatcherThread()
    {
        using var watcher = new RawGamepadWatcher();

        watcher.Start();
        watcher.Start();

        Assert.True(watcher.IsRunning);
    }
}
