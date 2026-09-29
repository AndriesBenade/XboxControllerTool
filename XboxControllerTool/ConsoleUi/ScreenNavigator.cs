namespace XboxControllerTool.ConsoleUi;

public sealed class ScreenNavigator
{
    private readonly Stack<IScreen> _stack = new();
    private readonly ConsoleFrameRenderer _renderer = new();

    private IReadOnlyList<ConsoleLine> _renderedLines = [];

    public ScreenNavigator(IScreen rootScreen)
    {
        _stack.Push(rootScreen);
        rootScreen.OnEnter();
    }

    public IScreen Current => _stack.Peek();

    public bool ShouldExit { get; private set; }

    public void Dispatch(MenuAction action)
    {
        if (action == MenuAction.None)
        {
            return;
        }

        var command = Current.HandleAction(action);
        Apply(command);
    }

    public void Render(bool force = false)
    {
        if (force)
        {
            _renderer.Invalidate();
        }

        LayoutMetrics.Refresh();

        // Kept so a mouse click can be turned back into the item drawn on that row.
        _renderedLines = Current.BuildLines();
        _renderer.Render(_renderedLines);
    }

    /// <summary>
    /// Handles a click on a console row, choosing whatever item is drawn there. Returns false for a
    /// row that holds no item - a heading, a border, a line of explanation - so a stray click on
    /// the frame does nothing rather than activating whatever happened to be highlighted.
    /// </summary>
    public bool TryClickRow(int row)
    {
        if (row < 0 || row >= _renderedLines.Count)
        {
            return false;
        }

        var itemIndex = _renderedLines[row].ItemIndex;

        if (itemIndex < 0)
        {
            return false;
        }

        Current.SelectItem(itemIndex);
        Dispatch(MenuAction.Confirm);
        return true;
    }

    private void Apply(NavigationCommand command)
    {
        switch (command.Kind)
        {
            case NavigationKind.Push when command.Target is not null:
                _stack.Push(command.Target);
                command.Target.OnEnter();
                _renderer.Invalidate();
                break;

            case NavigationKind.Pop when _stack.Count > 1:
                _stack.Pop();
                Current.OnEnter();
                _renderer.Invalidate();
                break;

            case NavigationKind.Exit:
                ShouldExit = true;
                break;
        }
    }
}
