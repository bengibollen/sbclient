using SbClient.Web.Protocol;

namespace SbClient.Web.Tests;

public class TelnetEchoStateResolverTests
{
    [Fact]
    public void ApplyRemoteEchoState_HidesInputWhenServerEchoIsEnabled()
    {
        var resolver = new TelnetEchoStateResolver();

        resolver.ApplyRemoteEchoState(isEchoEnabled: true);

        Assert.True(resolver.IsInputHidden);
        Assert.True(resolver.IsRemoteEchoEnabled);
    }

    [Fact]
    public void ApplyNegotiations_RestoresVisibleInputForWontEcho()
    {
        var resolver = new TelnetEchoStateResolver();

        resolver.ApplyNegotiations(
        [
            new TelnetNegotiationMessage(TelnetCommands.Wont, TelnetOptions.Echo, DateTimeOffset.UtcNow)
        ]);

        Assert.False(resolver.IsInputHidden);
        Assert.False(resolver.IsRemoteEchoEnabled);
    }

    [Fact]
    public void ApplyNegotiations_HidesInputForWillEcho()
    {
        var resolver = new TelnetEchoStateResolver();

        resolver.ApplyNegotiations(
        [
            new TelnetNegotiationMessage(TelnetCommands.Will, TelnetOptions.Echo, DateTimeOffset.UtcNow)
        ]);

        Assert.True(resolver.IsInputHidden);
        Assert.True(resolver.IsRemoteEchoEnabled);
    }

    [Fact]
    public void ApplyNegotiations_RestoresVisibleInputAfterPasswordPromptSequence()
    {
        var resolver = new TelnetEchoStateResolver();

        resolver.ApplyNegotiations(
        [
            new TelnetNegotiationMessage(TelnetCommands.Will, TelnetOptions.Echo, DateTimeOffset.UtcNow),
            new TelnetNegotiationMessage(TelnetCommands.Wont, TelnetOptions.Echo, DateTimeOffset.UtcNow)
        ]);

        Assert.False(resolver.IsInputHidden);
        Assert.False(resolver.IsRemoteEchoEnabled);
    }

    [Fact]
    public void ApplyNegotiations_MatchesObservedPasswordPromptPayload()
    {
        var resolver = new TelnetEchoStateResolver();
        var parser = new TelnetFrameParser();
        var payload = new byte[] { 13, 10, 80, 97, 115, 115, 119, 111, 114, 100, 58, 32, 255, 251, 1 };

        var result = parser.Process(payload);
        resolver.ApplyNegotiations(result.Negotiations);

        Assert.Equal("\r\nPassword: ", result.Text);
        Assert.True(resolver.IsInputHidden);
        Assert.True(resolver.IsRemoteEchoEnabled);
    }
}
