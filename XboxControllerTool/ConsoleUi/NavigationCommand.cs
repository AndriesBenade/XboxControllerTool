namespace XboxControllerTool.ConsoleUi;

public enum NavigationKind
{
    None,
    Push,
    Pop,
    Exit
}

public sealed record NavigationCommand(NavigationKind Kind, IScreen? Target = null)
{
    public static readonly NavigationCommand None = new(NavigationKind.None);
    public static readonly NavigationCommand Pop = new(NavigationKind.Pop);
    public static readonly NavigationCommand Exit = new(NavigationKind.Exit);

    public static NavigationCommand Push(IScreen target) => new(NavigationKind.Push, target);
}
