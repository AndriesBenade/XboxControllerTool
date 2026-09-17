namespace XboxControllerTool.ConsoleUi;

public sealed class ConsoleLine : IEquatable<ConsoleLine>
{
    public IReadOnlyList<ConsoleSegment> Segments { get; }

    public ConsoleLine(params ConsoleSegment[] segments)
    {
        Segments = segments;
    }

    public ConsoleLine(IReadOnlyList<ConsoleSegment> segments)
    {
        Segments = segments;
    }

    public static readonly ConsoleLine Empty = new(Array.Empty<ConsoleSegment>());

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
