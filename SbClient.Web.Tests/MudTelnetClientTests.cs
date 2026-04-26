using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SbClient.Web.Protocol;
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

    [Fact]
    public async Task InterpretAsync_AcceptsGmcpAndSendsHandshakeMessages()
    {
        await using var client = new MudTelnetClient(NullLogger<MudTelnetClient>.Instance);
        await using var stream = new MemoryStream();

        await client.AttachAsync(stream);
        await client.InterpretAsync(new byte[] { 255, 251, TelnetOptions.Gmcp });
        await client.SendGmcpCommandAsync("Core.Hello", "{\"client\":\"sbclient\",\"version\":\"1.0.0\"}");

        var bytes = stream.ToArray();
        Assert.Equal(255, bytes[0]);
        Assert.Equal(253, bytes[1]);
        Assert.Equal(TelnetOptions.Gmcp, bytes[2]);
        Assert.Contains(
            "Core.Hello {\"client\":\"sbclient\",\"version\":\"1.0.0\"}",
            Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task InterpretAsync_AcceptsNawsAndSendsWindowSizeUpdate()
    {
        await using var client = new MudTelnetClient(NullLogger<MudTelnetClient>.Instance);
        await using var stream = new MemoryStream();

        await client.AttachAsync(stream);
        await client.SendWindowSizeAsync(80, 24);

        Assert.Equal(
            new byte[]
            {
                255, 250, TelnetOptions.Naws, 0, 80, 0, 24, 255, 240
            },
            stream.ToArray());
    }

    [Fact]
    public async Task InterpretAsync_AcceptsPromptBoundaryNegotiation()
    {
        await using var client = new MudTelnetClient(NullLogger<MudTelnetClient>.Instance);
        await using var stream = new MemoryStream();

        await client.AttachAsync(stream);
        await client.InterpretAsync(new byte[] { 255, 251, TelnetOptions.EndOfRecord });

        Assert.Equal(new byte[] { 255, 253, TelnetOptions.EndOfRecord }, stream.ToArray());
    }
}
