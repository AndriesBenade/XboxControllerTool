namespace XboxControllerTool.ConsoleUi;

public interface IScreen
{
    IReadOnlyList<ConsoleLine> BuildLines();

    NavigationCommand HandleAction(MenuAction action);

    void OnEnter()
    {
    }

    /// <summary>Moves the highlight to a given item, so a mouse click can choose a row directly.</summary>
    void SelectItem(int index)
    {
    }
}
