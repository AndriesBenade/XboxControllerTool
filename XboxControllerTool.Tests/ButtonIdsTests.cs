using XboxControllerTool.Application;
using XboxControllerTool.Core;

namespace XboxControllerTool.Tests;

public class ButtonIdsTests
{
    [Fact]
    public void AnXInputIdentityRoundTripsThroughItsMask()
    {
        var id = ButtonIds.ForXInput((ushort)GamepadButton.RightThumb);

        Assert.True(ButtonIds.TryParseXInput(id, out var mask));
        Assert.Equal((ushort)GamepadButton.RightThumb, mask);
        Assert.False(ButtonIds.IsHid(id));
    }

    [Fact]
    public void AHidIdentityRoundTripsThroughItsUsage()
    {
        var id = ButtonIds.ForHid("045E-0B12", 15);

        Assert.True(ButtonIds.IsHid(id));
        Assert.True(ButtonIds.TryParseHidUsage(id, out var usage));
        Assert.Equal((ushort)15, usage);
        Assert.False(ButtonIds.TryParseXInput(id, out _));
    }

    [Fact]
    public void TheSamePhysicalButtonKeepsTheSameIdentityAcrossReconnects()
    {
        Assert.Equal(ButtonIds.ForHid("045E-0B12", 15), ButtonIds.ForHid("045E-0B12", 15));
        Assert.NotEqual(ButtonIds.ForHid("045E-0B12", 15), ButtonIds.ForHid("2DC8-3106", 15));
    }

    [Theory]
    [InlineData("")]
    [InlineData("xinput:")]
    [InlineData("xinput:ZZZZ")]
    [InlineData("hid:")]
    [InlineData("hid:045E-0B12:not-a-number")]
    [InlineData("something-else")]
    public void MalformedIdentitiesAreRejected(string id)
    {
        Assert.False(ButtonIds.IsWellFormed(id));
        Assert.False(CustomButtonService.IsMappable(id));
    }

    [Theory]
    [InlineData(GamepadButton.RightThumb, "R3")]
    [InlineData(GamepadButton.Guide, "GUIDE")]
    [InlineData(GamepadButton.Extra, "EXTRA")]
    public void KnownXInputButtonsGetReadableLabels(GamepadButton button, string expected)
    {
        Assert.Equal(expected, ButtonIds.LabelFor(ButtonIds.ForXInput((ushort)button)));
    }

    [Fact]
    public void AHidButtonIsLabelledByItsUsageNumber()
    {
        Assert.Equal("HID 15", ButtonIds.LabelFor(ButtonIds.ForHid("045E-0B12", 15)));
    }
}
