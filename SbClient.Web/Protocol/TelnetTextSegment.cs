namespace SbClient.Web.Protocol;

public sealed record TelnetTextSegment(
    string Text,
    bool EndsWithPromptBoundary);
