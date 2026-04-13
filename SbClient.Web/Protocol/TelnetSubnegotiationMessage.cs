namespace SbClient.Web.Protocol;

public sealed record TelnetSubnegotiationMessage(
    byte OptionCode,
    byte[] Payload,
    DateTimeOffset ReceivedAtUtc);
