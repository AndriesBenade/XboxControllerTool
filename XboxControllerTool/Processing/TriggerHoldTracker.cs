namespace XboxControllerTool.Processing;

public enum TriggerHoldTransition
{
    None,
    Activated,
    Deactivated
}

public sealed class TriggerHoldTracker
{
    private const byte TriggerThreshold = 30;

    public bool IsActive { get; private set; }

    public TriggerHoldTransition Update(byte triggerValue)
    {
        var isHeld = triggerValue > TriggerThreshold;

        if (isHeld == IsActive)
        {
            return TriggerHoldTransition.None;
        }

        IsActive = isHeld;
        return isHeld ? TriggerHoldTransition.Activated : TriggerHoldTransition.Deactivated;
    }
}
