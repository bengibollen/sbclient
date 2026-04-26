namespace SbClient.Web.Services;

public sealed class TerminalPromptTracker
{
    public bool IsPromptBoundaryPending { get; private set; }

    public string PrepareIncomingText(string text, bool endsWithPromptBoundary)
    {
        if (string.IsNullOrEmpty(text))
        {
            IsPromptBoundaryPending = endsWithPromptBoundary;
            return string.Empty;
        }

        var visibleText = IsPromptBoundaryPending && text[0] is not '\r' and not '\n'
            ? $"\n{text}"
            : text;
        IsPromptBoundaryPending = endsWithPromptBoundary;
        return visibleText;
    }

    public string BuildEchoedInput(string command)
    {
        IsPromptBoundaryPending = false;
        return $"{command}\n";
    }

    public void ConsumePromptBoundary()
    {
        IsPromptBoundaryPending = false;
    }

    public void Reset()
    {
        IsPromptBoundaryPending = false;
    }
}
