namespace SbClient.Web.Contracts;

public sealed record BrowserLearningSnapshot(
    DateTimeOffset GeneratedAtUtc,
    string DefaultHost,
    int DefaultPort,
    string BrowserWorkDescription,
    string ServerWorkDescription);
