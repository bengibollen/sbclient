using SbClient.Web.Protocol;

namespace SbClient.Web.Tests;

public class AnsiTranscriptParserTests
{
    [Fact]
    public void Parse_SplitsLinesAndPreservesAnsiStyles()
    {
        var transcript = "before \u001b[31mred\u001b[0m after\n\u001b[1;94mblue glyph \u001b[0m";

        var lines = AnsiTranscriptParser.Parse(transcript, scrollbackLineLimit: 10);

        Assert.Equal(2, lines.Count);
        Assert.Equal("before ", lines[0].Spans[0].Text);
        Assert.Equal("red", lines[0].Spans[1].Text);
        Assert.Equal("#d95757", lines[0].Spans[1].Style.Foreground);
        Assert.Equal("blue glyph ", lines[1].Spans[0].Text);
        Assert.True(lines[1].Spans[0].Style.Bold);
        Assert.Equal("#82aaff", lines[1].Spans[0].Style.Foreground);
    }

    [Fact]
    public void Parse_RespectsScrollbackLimit()
    {
        var transcript = "one\ntwo\nthree";

        var lines = AnsiTranscriptParser.Parse(transcript, scrollbackLineLimit: 2);

        Assert.Equal(2, lines.Count);
        Assert.Equal("two", lines[0].Spans[0].Text);
        Assert.Equal("three", lines[1].Spans[0].Text);
    }
}
