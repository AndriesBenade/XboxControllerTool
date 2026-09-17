using XboxControllerTool.Simulation;

namespace XboxControllerTool.Tests;

public sealed class RecordingMouse : IMouseInput
{
    public List<string> Actions { get; } = [];

    public List<(int Dx, int Dy)> Movements { get; } = [];

    public List<int> Scrolls { get; } = [];

    public void MoveRelative(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        Actions.Add(nameof(MoveRelative));
        Movements.Add((dx, dy));
    }

    public void LeftButtonDown() => Actions.Add(nameof(LeftButtonDown));

    public void LeftButtonUp() => Actions.Add(nameof(LeftButtonUp));

    public void RightButtonDown() => Actions.Add(nameof(RightButtonDown));

    public void RightButtonUp() => Actions.Add(nameof(RightButtonUp));

    public void MiddleButtonDown() => Actions.Add(nameof(MiddleButtonDown));

    public void MiddleButtonUp() => Actions.Add(nameof(MiddleButtonUp));

    public void Scroll(int wheelDelta)
    {
        if (wheelDelta == 0)
        {
            return;
        }

        Actions.Add(nameof(Scroll));
        Scrolls.Add(wheelDelta);
    }
}
