namespace XboxControllerTool.ConsoleUi;

public readonly record struct ConsoleSegment(string Text, ConsoleColor? Foreground = null, ConsoleColor? Background = null)
{
    public static implicit operator ConsoleSegment(string text) => new(text);
}
