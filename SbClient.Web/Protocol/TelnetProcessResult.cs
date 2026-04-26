namespace SbClient.Web.Protocol;

public sealed record TelnetProcessResult(
    IReadOnlyList<TelnetTextSegment> TextSegments,
    IReadOnlyList<TelnetSubnegotiationMessage> Subnegotiations,
    IReadOnlyList<byte[]> NegotiationResponses,
    IReadOnlyList<TelnetNegotiationMessage> Negotiations)
{
    public string Text => string.Concat(TextSegments.Select(segment => segment.Text));
}
