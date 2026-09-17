using XboxControllerTool.Processing;

namespace XboxControllerTool.Tests;

public class TriggerHoldTrackerTests
{
    [Fact]
    public void Update_TriggerBeyondThreshold_ActivatesOnce()
    {
        var tracker = new TriggerHoldTracker();

        var first = tracker.Update(200);
        var second = tracker.Update(200);

        Assert.Equal(TriggerHoldTransition.Activated, first);
        Assert.Equal(TriggerHoldTransition.None, second);
        Assert.True(tracker.IsActive);
    }

    [Fact]
    public void Update_TriggerReleased_DeactivatesOnce()
    {
        var tracker = new TriggerHoldTracker();
        tracker.Update(200);

        var first = tracker.Update(0);
        var second = tracker.Update(0);

        Assert.Equal(TriggerHoldTransition.Deactivated, first);
        Assert.Equal(TriggerHoldTransition.None, second);
        Assert.False(tracker.IsActive);
    }

    [Fact]
    public void Update_ValueAtOrBelowThreshold_DoesNotActivate()
    {
        var tracker = new TriggerHoldTracker();

        var result = tracker.Update(30);

        Assert.Equal(TriggerHoldTransition.None, result);
        Assert.False(tracker.IsActive);
    }

    [Fact]
    public void Update_HoldingTriggerAcrossManyTicksFiresNotificationOnlyOnce()
    {
        var tracker = new TriggerHoldTracker();
        var activationCount = 0;

        for (var i = 0; i < 1000; i++)
        {
            if (tracker.Update(255) == TriggerHoldTransition.Activated)
            {
                activationCount++;
            }
        }

        Assert.Equal(1, activationCount);
    }

    [Fact]
    public void Update_TwoIndependentTrackers_DoNotShareState()
    {
        var leftTrigger = new TriggerHoldTracker();
        var rightTrigger = new TriggerHoldTracker();

        leftTrigger.Update(255);

        Assert.True(leftTrigger.IsActive);
        Assert.False(rightTrigger.IsActive);
    }
}
