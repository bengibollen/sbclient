namespace SbClient.Web.Models;

public sealed record TerminalStyle(
    string? Foreground = null,
    string? Background = null,
    bool Bold = false,
    bool Underline = false)
{
    public static TerminalStyle Default { get; } = new();
}
