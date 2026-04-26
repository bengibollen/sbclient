using SbClient.Web.Services;

namespace SbClient.Web.Tests;

public class TerminalPromptTrackerTests
{
    [Fact]
    public void PrepareIncomingText_RecordsPromptBoundaryAtEndOfSegment()
    {
        var tracker = new TerminalPromptTracker();

        var visibleText = tracker.PrepareIncomingText("\r\n> ", endsWithPromptBoundary: true);

        Assert.Equal("\r\n> ", visibleText);
        Assert.True(tracker.IsPromptBoundaryPending);
    }

    [Fact]
    public void BuildEchoedInput_ConsumesPromptBoundaryWithoutDuplicatingPrompt()
    {
        var tracker = new TerminalPromptTracker();
        tracker.PrepareIncomingText("> ", endsWithPromptBoundary: true);

        var echoedInput = tracker.BuildEchoedInput("look");

        Assert.Equal("look\n", echoedInput);
        Assert.False(tracker.IsPromptBoundaryPending);
    }

    [Fact]
    public void PrepareIncomingText_InsertsLineBreakBeforeAsyncOutputAfterPromptBoundary()
    {
        var tracker = new TerminalPromptTracker();
        tracker.PrepareIncomingText("> ", endsWithPromptBoundary: true);

        var visibleText = tracker.PrepareIncomingText("A bat flaps past.\n", endsWithPromptBoundary: false);

        Assert.Equal("\nA bat flaps past.\n", visibleText);
        Assert.False(tracker.IsPromptBoundaryPending);
    }

    [Fact]
    public void PrepareIncomingText_DoesNotInsertExtraLineBreakWhenOutputAlreadyStartsOnNewLine()
    {
        var tracker = new TerminalPromptTracker();
        tracker.PrepareIncomingText("> ", endsWithPromptBoundary: true);

        var visibleText = tracker.PrepareIncomingText("\r\nNorth\n", endsWithPromptBoundary: false);

        Assert.Equal("\r\nNorth\n", visibleText);
        Assert.False(tracker.IsPromptBoundaryPending);
    }
}
