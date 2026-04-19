namespace SbClient.Web.Protocol;

public sealed record TelnetNegotiationMessage(
    byte Command,
    byte OptionCode,
    DateTimeOffset ObservedAtUtc);
