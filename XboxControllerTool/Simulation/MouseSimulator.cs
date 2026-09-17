using System.Runtime.InteropServices;

namespace XboxControllerTool.Simulation;

public sealed class MouseSimulator : IMouseInput
{
    public void MoveRelative(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        Send(new Input
        {
            type = NativeInput.InputMouse,
            u = new InputUnion { Mouse = new MouseInput { dx = dx, dy = dy, dwFlags = NativeInput.MouseEventMove } }
        });
    }

    public void LeftButtonDown() => SendMouseFlag(NativeInput.MouseEventLeftDown);
    public void LeftButtonUp() => SendMouseFlag(NativeInput.MouseEventLeftUp);
    public void RightButtonDown() => SendMouseFlag(NativeInput.MouseEventRightDown);
    public void RightButtonUp() => SendMouseFlag(NativeInput.MouseEventRightUp);

    public void Scroll(int wheelDelta)
    {
        if (wheelDelta == 0)
        {
            return;
        }

        Send(new Input
        {
            type = NativeInput.InputMouse,
            u = new InputUnion { Mouse = new MouseInput { mouseData = unchecked((uint)wheelDelta), dwFlags = NativeInput.MouseEventWheel } }
        });
    }

    private static void SendMouseFlag(uint flag)
    {
        Send(new Input
        {
            type = NativeInput.InputMouse,
            u = new InputUnion { Mouse = new MouseInput { dwFlags = flag } }
        });
    }

    private static void Send(Input input)
    {
        var inputs = new[] { input };
        NativeInput.SendInput(1, inputs, Marshal.SizeOf<Input>());
    }
}
