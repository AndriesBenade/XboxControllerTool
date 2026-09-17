namespace XboxControllerTool.ConsoleUi;

public interface IScreen
{
    IReadOnlyList<ConsoleLine> BuildLines();

    NavigationCommand HandleAction(MenuAction action);

    void OnEnter()
    {
    }
}
