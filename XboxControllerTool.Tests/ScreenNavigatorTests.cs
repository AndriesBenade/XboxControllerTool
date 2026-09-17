using XboxControllerTool.ConsoleUi;

namespace XboxControllerTool.Tests;

public class ScreenNavigatorTests
{
    private sealed class RecordingListScreen : ListMenuScreen
    {
        public int EnterCount { get; private set; }

        protected override int ItemCount => 3;

        public IScreen? PushOnConfirm { get; set; }

        public override IReadOnlyList<ConsoleLine> BuildLines() => [new ConsoleLine($"Item {SelectedIndex}")];

        public override void OnEnter()
        {
            base.OnEnter();
            EnterCount++;
        }

        public int LastConfirmedIndex { get; private set; } = -1;

        protected override NavigationCommand OnConfirm(int index)
        {
            LastConfirmedIndex = index;
            return PushOnConfirm is { } target ? NavigationCommand.Push(target) : NavigationCommand.None;
        }
    }

    private sealed class LeafScreen : IScreen
    {
        public IReadOnlyList<ConsoleLine> BuildLines() => [new ConsoleLine("Leaf")];

        public NavigationCommand HandleAction(MenuAction action) =>
            action == MenuAction.Cancel ? NavigationCommand.Pop : NavigationCommand.None;
    }

    [Fact]
    public void Dispatch_Down_MovesSelectionForward()
    {
        var root = new RecordingListScreen();
        var navigator = new ScreenNavigator(root);

        navigator.Dispatch(MenuAction.Down);
        navigator.Dispatch(MenuAction.Confirm);

        Assert.Equal(1, root.LastConfirmedIndex);
    }

    [Fact]
    public void Dispatch_DownWrapsAroundAtEndOfList()
    {
        var root = new RecordingListScreen();
        var navigator = new ScreenNavigator(root);

        navigator.Dispatch(MenuAction.Down);
        navigator.Dispatch(MenuAction.Down);
        navigator.Dispatch(MenuAction.Down);
        navigator.Dispatch(MenuAction.Confirm);

        Assert.Equal(0, root.LastConfirmedIndex);
    }

    [Fact]
    public void Dispatch_Push_MakesTargetTheCurrentScreen()
    {
        var leaf = new LeafScreen();
        var root = new RecordingListScreen { PushOnConfirm = leaf };
        var navigator = new ScreenNavigator(root);

        navigator.Dispatch(MenuAction.Confirm);

        Assert.Same(leaf, navigator.Current);
    }

    [Fact]
    public void Dispatch_CancelFromLeaf_ReturnsToRootAndReenters()
    {
        var leaf = new LeafScreen();
        var root = new RecordingListScreen { PushOnConfirm = leaf };
        var navigator = new ScreenNavigator(root);

        navigator.Dispatch(MenuAction.Confirm);
        navigator.Dispatch(MenuAction.Cancel);

        Assert.Same(root, navigator.Current);
        Assert.Equal(2, root.EnterCount);
    }

    [Fact]
    public void Dispatch_CancelAtRoot_DoesNotPopBelowRoot()
    {
        var root = new RecordingListScreen();
        var navigator = new ScreenNavigator(root);

        navigator.Dispatch(MenuAction.Cancel);

        Assert.Same(root, navigator.Current);
    }
}
