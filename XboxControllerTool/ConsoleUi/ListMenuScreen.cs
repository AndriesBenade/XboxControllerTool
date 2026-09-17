namespace XboxControllerTool.ConsoleUi;

public abstract class ListMenuScreen : IScreen
{
    protected abstract int ItemCount { get; }

    protected int SelectedIndex { get; private set; }

    public abstract IReadOnlyList<ConsoleLine> BuildLines();

    public virtual NavigationCommand HandleAction(MenuAction action)
    {
        if (ItemCount == 0)
        {
            return action == MenuAction.Cancel ? OnCancel() : NavigationCommand.None;
        }

        switch (action)
        {
            case MenuAction.Up:
                SelectedIndex = (SelectedIndex - 1 + ItemCount) % ItemCount;
                return NavigationCommand.None;
            case MenuAction.Down:
                SelectedIndex = (SelectedIndex + 1) % ItemCount;
                return NavigationCommand.None;
            case MenuAction.Left:
                return OnAdjust(SelectedIndex, -1);
            case MenuAction.Right:
                return OnAdjust(SelectedIndex, 1);
            case MenuAction.Confirm:
                return OnConfirm(SelectedIndex);
            case MenuAction.Cancel:
                return OnCancel();
            default:
                return NavigationCommand.None;
        }
    }

    public virtual void OnEnter() => SelectedIndex = 0;

    protected virtual NavigationCommand OnConfirm(int index) => NavigationCommand.None;

    protected virtual NavigationCommand OnCancel() => NavigationCommand.Pop;

    protected virtual NavigationCommand OnAdjust(int index, int direction) => NavigationCommand.None;
}
