namespace XboxControllerTool.Simulation;

public interface IKeyboardInput
{
    void StartVoiceTyping();
    void StopVoiceTyping();
    void SendShowDesktop();
    void SendEscape();
    void SendAltLeft();
    void SendAltRight();
    void SendEnter();
    void ArrowLeftDown();
    void ArrowLeftUp();
    void ArrowRightDown();
    void ArrowRightUp();
    void BackspaceDown();
    void BackspaceUp();
}
