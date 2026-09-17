namespace XboxControllerTool.ConsoleUi;

public static class FieldRow
{
    public static ConsoleSegment[] Build(string label, int labelWidth, ConsoleSegment value, int valueWidth = 0)
    {
        var valueText = valueWidth > 0 ? value.Text.PadRight(valueWidth) : value.Text;

        return
        [
            new ConsoleSegment(label.PadRight(labelWidth), ConsoleTheme.Label),
            value with { Text = valueText }
        ];
    }
}
