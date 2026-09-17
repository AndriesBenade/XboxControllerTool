using XboxControllerTool.Core;

namespace XboxControllerTool.Input;

public enum SelectedControllerAvailability
{
    NotApplicable,
    Available,
    Unavailable
}

public sealed class ControllerSelectionChangedEventArgs(ControllerSelectionMode mode, SelectedControllerAvailability availability) : EventArgs
{
    public ControllerSelectionMode Mode { get; } = mode;
    public SelectedControllerAvailability Availability { get; } = availability;
}

public sealed class ControllerSelectionService
{
    private ControllerSelectionMode _mode;
    private ControllerIdentity? _selectedIdentity;
    private bool _isWaitingForSelection;

    public event EventHandler<ControllerSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler? WaitingForSelectionStarted;
    public event EventHandler? WaitingForSelectionCancelled;

    public ControllerSelectionMode Mode => _mode;
    public ControllerIdentity? SelectedIdentity => _selectedIdentity;
    public bool IsWaitingForSelection => _isWaitingForSelection;

    public void Restore(ControllerSelectionMode mode, ControllerIdentity? identity)
    {
        _mode = mode;
        _selectedIdentity = mode == ControllerSelectionMode.SpecificController ? identity : null;
    }

    public void BeginSelectSpecificController()
    {
        _isWaitingForSelection = true;
        WaitingForSelectionStarted?.Invoke(this, EventArgs.Empty);
    }

    public void CancelSelection()
    {
        if (!_isWaitingForSelection)
        {
            return;
        }

        _isWaitingForSelection = false;
        WaitingForSelectionCancelled?.Invoke(this, EventArgs.Empty);
    }

    public void ResetToAllControllers()
    {
        _isWaitingForSelection = false;
        _mode = ControllerSelectionMode.AllControllers;
        _selectedIdentity = null;
        SelectionChanged?.Invoke(this, new ControllerSelectionChangedEventArgs(_mode, SelectedControllerAvailability.NotApplicable));
    }

    public void ProcessSnapshots(IReadOnlyList<ControllerSnapshot> snapshots, IReadOnlyList<ButtonTransitions> transitions)
    {
        if (!_isWaitingForSelection)
        {
            return;
        }

        for (var i = 0; i < snapshots.Count; i++)
        {
            if (!snapshots[i].State.IsConnected)
            {
                continue;
            }

            if (transitions[i].WasPressed(GamepadButton.Start))
            {
                _mode = ControllerSelectionMode.SpecificController;
                _selectedIdentity = new ControllerIdentity(snapshots[i].State.UserIndex, snapshots[i].Capabilities);
                _isWaitingForSelection = false;
                SelectionChanged?.Invoke(this, new ControllerSelectionChangedEventArgs(_mode, SelectedControllerAvailability.Available));
                return;
            }
        }
    }

    public SelectedControllerAvailability GetSelectedControllerAvailability(IReadOnlyList<ControllerSnapshot> snapshots)
    {
        if (_mode != ControllerSelectionMode.SpecificController || _selectedIdentity is not { } identity)
        {
            return SelectedControllerAvailability.NotApplicable;
        }

        foreach (var snapshot in snapshots)
        {
            if (snapshot.State.IsConnected && identity.Matches(snapshot.State.UserIndex, snapshot.Capabilities))
            {
                return SelectedControllerAvailability.Available;
            }
        }

        return SelectedControllerAvailability.Unavailable;
    }

    public IReadOnlyList<ControllerState> FilterAccepted(IReadOnlyList<ControllerSnapshot> snapshots)
    {
        var accepted = new List<ControllerState>(snapshots.Count);

        foreach (var snapshot in snapshots)
        {
            if (!snapshot.State.IsConnected)
            {
                continue;
            }

            if (_mode == ControllerSelectionMode.AllControllers)
            {
                accepted.Add(snapshot.State);
                continue;
            }

            if (_selectedIdentity is { } identity && identity.Matches(snapshot.State.UserIndex, snapshot.Capabilities))
            {
                accepted.Add(snapshot.State);
            }
        }

        return accepted;
    }
}
