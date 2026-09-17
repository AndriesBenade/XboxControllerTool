namespace XboxControllerTool.ConsoleUi;

public sealed class ScreenNavigator
{
    private readonly Stack<IScreen> _stack = new();
    private readonly ConsoleFrameRenderer _renderer = new();

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
        _renderer.Render(Current.BuildLines());
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
