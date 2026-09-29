namespace XboxControllerTool.ConsoleUi;

public sealed class ConsoleLine : IEquatable<ConsoleLine>
{
    public IReadOnlyList<ConsoleSegment> Segments { get; }

    /// <summary>
    /// Which selectable item this row draws, or -1 for a row that cannot be chosen. It is what lets
    /// a mouse click on a row find the item under it. Deliberately outside equality: it never
    /// changes what is drawn, and the renderer compares lines to decide what to redraw.
    /// </summary>
    public int ItemIndex { get; init; } = -1;

    public ConsoleLine(params ConsoleSegment[] segments)
    {
        Segments = segments;
    }

    public ConsoleLine(IReadOnlyList<ConsoleSegment> segments)
    {
        Segments = segments;
    }

    public static readonly ConsoleLine Empty = new(Array.Empty<ConsoleSegment>());

    public ConsoleLine ForItem(int itemIndex) => new(Segments) { ItemIndex = itemIndex };

    public static implicit operator ConsoleLine(string text) => new(new ConsoleSegment(text));

    public bool Equals(ConsoleLine? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Segments.SequenceEqual(other.Segments);
    }

    public override bool Equals(object? obj) => Equals(obj as ConsoleLine);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var segment in Segments)
        {
            hash.Add(segment);
        }

        return hash.ToHashCode();
    }
}
