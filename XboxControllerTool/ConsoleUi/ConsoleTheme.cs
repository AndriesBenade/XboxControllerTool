namespace XboxControllerTool.ConsoleUi;

public static class ConsoleTheme
{
    public static Theme Current { get; private set; } = ThemeCatalog.Dark;

    public static event EventHandler? Changed;

    public static void Apply(Theme theme)
    {
        if (ReferenceEquals(theme, Current))
        {
            return;
        }

        Current = theme;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static ConsoleColor Background => Current.Background;
    public static ConsoleColor Title => Current.Title;
    public static ConsoleColor BandFrame => Current.BandFrame;
    public static ConsoleColor PanelFrame => Current.PanelFrame;
    public static ConsoleColor PanelTitle => Current.PanelTitle;
    public static ConsoleColor Label => Current.Label;
    public static ConsoleColor Text => Current.Text;
    public static ConsoleColor Value => Current.Value;
    public static ConsoleColor Focus => Current.Focus;
    public static ConsoleColor ButtonFrame => Current.ButtonFrame;
    public static ConsoleColor ButtonLabel => Current.ButtonLabel;
    public static ConsoleColor Positive => Current.Positive;
    public static ConsoleColor Active => Current.Active;
    public static ConsoleColor Negative => Current.Negative;
}
