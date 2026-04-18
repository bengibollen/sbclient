using Microsoft.Extensions.Options;
using SbClient.Web.Models;
using SbClient.Web.Services;

namespace SbClient.Web.Tests;

public class BrowserLearningSnapshotFactoryTests
{
    [Fact]
    public void Create_UsesGatewayDefaultsAndExplainsTheBoundary()
    {
        var options = Options.Create(new MudGatewayOptions
        {
            Host = "discworld.starturtle.net",
            Port = 4242
        });
        var factory = new BrowserLearningSnapshotFactory(options);

        var snapshot = factory.Create();

        Assert.Equal("discworld.starturtle.net", snapshot.DefaultHost);
        Assert.Equal(4242, snapshot.DefaultPort);
        Assert.Contains("browser", snapshot.BrowserWorkDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MudClientSession", snapshot.ServerWorkDescription);
    }
}
