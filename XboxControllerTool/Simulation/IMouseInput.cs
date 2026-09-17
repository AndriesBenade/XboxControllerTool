namespace XboxControllerTool.Simulation;

public interface IMouseInput
{
    void MoveRelative(int dx, int dy);
    void LeftButtonDown();
    void LeftButtonUp();
    void RightButtonDown();
    void RightButtonUp();
    void Scroll(int wheelDelta);
}
