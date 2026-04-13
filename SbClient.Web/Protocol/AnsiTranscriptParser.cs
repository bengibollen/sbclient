using System.Text;
using SbClient.Web.Models;

namespace SbClient.Web.Protocol;

public static class AnsiTranscriptParser
{
    private const char Escape = '\u001b';

    private static readonly IReadOnlyDictionary<int, string> Foregrounds = new Dictionary<int, string>
    {
        [30] = "#1f2430",
        [31] = "#d95757",
        [32] = "#5faf5f",
        [33] = "#d7af5f",
        [34] = "#5f87d7",
        [35] = "#af5fd7",
        [36] = "#5fd7d7",
        [37] = "#d7d7d7",
        [90] = "#7a8294",
        [91] = "#ff6b6b",
        [92] = "#8ee38e",
        [93] = "#ffd479",
        [94] = "#82aaff",
        [95] = "#d4a5ff",
        [96] = "#86e1fc",
        [97] = "#f5f7ff"
    };

    private static readonly IReadOnlyDictionary<int, string> Backgrounds = new Dictionary<int, string>
    {
        [40] = "#1f2430",
        [41] = "#7f1d1d",
        [42] = "#14532d",
        [43] = "#78350f",
        [44] = "#1d4ed8",
        [45] = "#701a75",
        [46] = "#155e75",
        [47] = "#d7d7d7",
        [100] = "#475569",
        [101] = "#dc2626",
        [102] = "#16a34a",
        [103] = "#ca8a04",
        [104] = "#2563eb",
        [105] = "#c026d3",
        [106] = "#0891b2",
        [107] = "#f8fafc"
    };

    public static IReadOnlyList<TerminalLine> Parse(string transcript, int scrollbackLineLimit)
    {
        var lines = new List<TerminalLine>();
        var spans = new List<TerminalSpan>();
        var text = new StringBuilder();
        var style = TerminalStyle.Default;
        var escape = new StringBuilder();
        var inEscape = false;

        foreach (var character in transcript)
        {
            if (inEscape)
            {
                escape.Append(character);
                if (character is >= '@' and <= '~' && !(escape.Length == 1 && character == '['))
                {
                    ApplyEscapeSequence(escape.ToString(), ref style);
                    escape.Clear();
                    inEscape = false;
                }

                continue;
            }

            if (character == Escape)
            {
                FlushText(text, spans, style);
                inEscape = true;
                continue;
            }

            if (character == '\r')
            {
                continue;
            }

            if (character == '\n')
            {
                FlushText(text, spans, style);
                lines.Add(new TerminalLine(spans.ToArray()));
                spans.Clear();
                continue;
            }

            text.Append(character);
        }

        FlushText(text, spans, style);
        if (spans.Count > 0 || lines.Count == 0)
        {
            lines.Add(new TerminalLine(spans.ToArray()));
        }

        return lines.Count <= scrollbackLineLimit
            ? lines
            : lines.Skip(lines.Count - scrollbackLineLimit).ToArray();
    }

    private static void ApplyEscapeSequence(string sequence, ref TerminalStyle style)
    {
        if (!sequence.StartsWith('[') || !sequence.EndsWith('m'))
        {
            return;
        }

        var parameters = sequence[1..^1]
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parameters.Length == 0)
        {
            style = TerminalStyle.Default;
            return;
        }

        foreach (var parameter in parameters)
        {
            if (!int.TryParse(parameter, out var code))
            {
                continue;
            }

            style = code switch
            {
                0 => TerminalStyle.Default,
                1 => style with { Bold = true },
                4 => style with { Underline = true },
                22 => style with { Bold = false },
                24 => style with { Underline = false },
                39 => style with { Foreground = null },
                49 => style with { Background = null },
                _ when Foregrounds.TryGetValue(code, out var foreground) => style with { Foreground = foreground },
                _ when Backgrounds.TryGetValue(code, out var background) => style with { Background = background },
                _ => style
            };
        }
    }

    private static void FlushText(StringBuilder text, List<TerminalSpan> spans, TerminalStyle style)
    {
        if (text.Length == 0)
        {
            return;
        }

        spans.Add(new TerminalSpan(text.ToString(), style));
        text.Clear();
    }
}
