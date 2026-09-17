using XboxControllerTool.Core;
using XboxControllerTool.Input;

namespace XboxControllerTool.Tests;

public class ControllerSelectionServiceTests
{
    private static ControllerSnapshot Connected(int userIndex, GamepadButton buttons = GamepadButton.None, byte subType = 1) =>
        new(new ControllerState(userIndex, true, 1, buttons, 0, 0, 0, 0, 0, 0), new ControllerCapabilities(1, subType, 0));

    private static ControllerSnapshot Disconnected(int userIndex) =>
        new(ControllerState.Disconnected(userIndex), ControllerCapabilities.Unknown);

    private static ButtonTransitions[] TransitionsFrom(ControllerSnapshot[] previous, ControllerSnapshot[] current)
    {
        var transitions = new ButtonTransitions[current.Length];
        for (var i = 0; i < current.Length; i++)
        {
            transitions[i] = ButtonEdgeDetector.Detect(previous[i].State.Buttons, current[i].State.Buttons);
        }

        return transitions;
    }

    [Fact]
    public void DefaultMode_IsAllControllers()
    {
        var service = new ControllerSelectionService();

        Assert.Equal(ControllerSelectionMode.AllControllers, service.Mode);
    }

    [Fact]
    public void AllControllersMode_AcceptsEveryConnectedController()
    {
        var service = new ControllerSelectionService();
        var snapshots = new[] { Connected(0), Connected(1), Disconnected(2), Disconnected(3) };

        var accepted = service.FilterAccepted(snapshots);

        Assert.Equal(2, accepted.Count);
    }

    [Fact]
    public void SpecificControllerMode_OnlyAcceptsMatchingIdentity()
    {
        var service = new ControllerSelectionService();
        var identity = new ControllerIdentity(1, new ControllerCapabilities(1, 1, 0));
        service.Restore(ControllerSelectionMode.SpecificController, identity);

        var snapshots = new[] { Connected(0), Connected(1), Disconnected(2), Disconnected(3) };
        var accepted = service.FilterAccepted(snapshots);

        Assert.Single(accepted);
        Assert.Equal(1, accepted[0].UserIndex);
    }

    [Fact]
    public void SpecificControllerMode_RejectsAllWhenSelectedControllerDisconnected()
    {
        var service = new ControllerSelectionService();
        var identity = new ControllerIdentity(1, new ControllerCapabilities(1, 1, 0));
        service.Restore(ControllerSelectionMode.SpecificController, identity);

        var snapshots = new[] { Connected(0), Disconnected(1), Disconnected(2), Disconnected(3) };
        var accepted = service.FilterAccepted(snapshots);

        Assert.Empty(accepted);
    }

    [Fact]
    public void BeginSelectSpecificController_ThenStartPress_SelectsThatController()
    {
        var service = new ControllerSelectionService();
        service.BeginSelectSpecificController();

        var previous = new[] { Disconnected(0), Connected(1), Disconnected(2), Disconnected(3) };
        var current = new[] { Disconnected(0), Connected(1, GamepadButton.Start), Disconnected(2), Disconnected(3) };

        service.ProcessSnapshots(current, TransitionsFrom(previous, current));

        Assert.Equal(ControllerSelectionMode.SpecificController, service.Mode);
        Assert.Equal(1, service.SelectedIdentity!.Value.UserIndex);
        Assert.False(service.IsWaitingForSelection);
    }

    [Fact]
    public void BackButton_DoesNotResetSpecificSelection()
    {
        var service = new ControllerSelectionService();
        var identity = new ControllerIdentity(0, new ControllerCapabilities(1, 1, 0));
        service.Restore(ControllerSelectionMode.SpecificController, identity);

        var previous = new[] { Connected(0), Disconnected(1), Disconnected(2), Disconnected(3) };
        var current = new[] { Connected(0, GamepadButton.Back), Disconnected(1), Disconnected(2), Disconnected(3) };

        service.ProcessSnapshots(current, TransitionsFrom(previous, current));

        Assert.Equal(ControllerSelectionMode.SpecificController, service.Mode);
        Assert.Equal(identity, service.SelectedIdentity);
    }

    [Fact]
    public void ResetToAllControllers_OnlyHappensThroughExplicitCall()
    {
        var service = new ControllerSelectionService();
        service.Restore(ControllerSelectionMode.SpecificController, new ControllerIdentity(0, new ControllerCapabilities(1, 1, 0)));

        service.ResetToAllControllers();

        Assert.Equal(ControllerSelectionMode.AllControllers, service.Mode);
        Assert.Null(service.SelectedIdentity);
    }

    [Fact]
    public void GetSelectedControllerAvailability_ReportsUnavailableWhenNotConnected()
    {
        var service = new ControllerSelectionService();
        service.Restore(ControllerSelectionMode.SpecificController, new ControllerIdentity(2, new ControllerCapabilities(1, 1, 0)));

        var availability = service.GetSelectedControllerAvailability([Connected(0), Connected(1), Disconnected(2), Disconnected(3)]);

        Assert.Equal(SelectedControllerAvailability.Unavailable, availability);
    }

    [Fact]
    public void GetSelectedControllerAvailability_NotApplicableInAllControllersMode()
    {
        var service = new ControllerSelectionService();

        var availability = service.GetSelectedControllerAvailability([Connected(0)]);

        Assert.Equal(SelectedControllerAvailability.NotApplicable, availability);
    }
}
