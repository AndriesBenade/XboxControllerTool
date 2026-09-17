namespace XboxControllerTool.ConsoleUi;

public static class LayoutMetrics
{
    private const int ComfortableRowBudget = 34;

    public static bool Compact { get; private set; }

    public static void Refresh()
    {
        try
        {
            Compact = Console.WindowHeight < ComfortableRowBudget;
        }
        catch (IOException)
        {
            Compact = false;
        }
    }

    public static void PanelPad(List<ConsoleLine> lines)
    {
        if (!Compact)
        {
            lines.Add(Panel.Blank());
        }
    }

    public static void Gap(List<ConsoleLine> lines)
    {
        if (!Compact)
        {
            lines.Add(ConsoleLine.Empty);
        }
    }
}
