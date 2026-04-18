using Microsoft.Extensions.Options;
using SbClient.Web.Contracts;
using SbClient.Web.Models;

namespace SbClient.Web.Services;

public sealed class BrowserLearningSnapshotFactory
{
    private readonly MudGatewayOptions _options;

    public BrowserLearningSnapshotFactory(IOptions<MudGatewayOptions> options)
    {
        _options = options.Value;
    }

    public BrowserLearningSnapshot Create()
    {
        return new BrowserLearningSnapshot(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            DefaultHost: _options.Host,
            DefaultPort: _options.Port,
            BrowserWorkDescription: "This page's component code runs in the browser through Blazor WebAssembly.",
            ServerWorkDescription: "MudClientSession, telnet parsing, and the TCP connection to the MUD stay on the ASP.NET Core server.");
    }
}
