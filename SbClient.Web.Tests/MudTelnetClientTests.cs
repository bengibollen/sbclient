using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SbClient.Web.Services;

namespace SbClient.Web.Tests;

public class MudTelnetClientTests
{
    [Fact]
    public async Task InterpretAsync_AcceptsServerEchoAndRaisesVisibleState()
    {
        await using var client = new MudTelnetClient(NullLogger<MudTelnetClient>.Instance);
        await using var stream = new MemoryStream();
        bool? remoteEchoEnabled = null;

        client.RemoteEchoChanged += enabled => remoteEchoEnabled = enabled;

        await client.AttachAsync(stream);
        await client.InterpretAsync(new byte[] { 255, 251, 1 });

        Assert.True(remoteEchoEnabled);
        Assert.True(client.IsRemoteEchoEnabled);
        Assert.Equal(new byte[] { 255, 253, 1 }, stream.ToArray());
    }

    [Fact]
    public async Task InterpretAsync_DisablesServerEchoAndRaisesHiddenState()
    {
        await using var client = new MudTelnetClient(NullLogger<MudTelnetClient>.Instance);
        await using var stream = new MemoryStream();
        bool? remoteEchoEnabled = null;

        client.RemoteEchoChanged += enabled => remoteEchoEnabled = enabled;

        await client.AttachAsync(stream);
        await client.InterpretAsync(new byte[] { 255, 252, 1 });

        Assert.False(remoteEchoEnabled);
        Assert.False(client.IsRemoteEchoEnabled);
        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public async Task SendAsync_WritesCommandWithLineEnding()
    {
        await using var client = new MudTelnetClient(NullLogger<MudTelnetClient>.Instance);
        await using var stream = new MemoryStream();

        await client.AttachAsync(stream);
        await client.SendAsync("look");

        Assert.Equal(Encoding.UTF8.GetBytes("look\r\n"), stream.ToArray());
    }
}
