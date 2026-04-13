namespace SbClient.Web.Models;

public sealed record MudMediaItem(
    string Kind,
    string Title,
    string? Source,
    string? Description,
    DateTimeOffset ReceivedAtUtc);
