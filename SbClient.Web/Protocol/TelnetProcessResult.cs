namespace SbClient.Web.Protocol;

public sealed record TelnetProcessResult(
    string Text,
    IReadOnlyList<TelnetSubnegotiationMessage> Subnegotiations,
    IReadOnlyList<byte[]> NegotiationResponses,
    IReadOnlyList<TelnetNegotiationMessage> Negotiations);
