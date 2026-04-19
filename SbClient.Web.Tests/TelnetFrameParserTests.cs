using System.Text;
using SbClient.Web.Protocol;

namespace SbClient.Web.Tests;

public class TelnetFrameParserTests
{
    [Fact]
    public void Process_StripsUnsupportedNegotiationAndReturnsRefusal()
    {
        var parser = new TelnetFrameParser();
        var buffer = new byte[] { 255, 253, 1, (byte)'O', (byte)'K' };

        var result = parser.Process(buffer);

        Assert.Equal("OK", result.Text);
        Assert.Single(result.NegotiationResponses);
        Assert.Equal(new byte[] { 255, 252, 1 }, result.NegotiationResponses[0]);
        Assert.False(parser.OptionState.GetLocalOptionState(TelnetOptions.Echo));
    }

    [Fact]
    public void Process_CollectsSubnegotiationFrames()
    {
        var parser = new TelnetFrameParser();
        var payload = Encoding.UTF8.GetBytes("map:preview");
        var buffer = new byte[payload.Length + 5];
        buffer[0] = 255;
        buffer[1] = 250;
        buffer[2] = 201;
        payload.CopyTo(buffer, 3);
        buffer[^2] = 255;
        buffer[^1] = 240;

        var result = parser.Process(buffer);

        var message = Assert.Single(result.Subnegotiations);
        Assert.Equal(201, message.OptionCode);
        Assert.Equal("map:preview", Encoding.UTF8.GetString(message.Payload));
    }

    [Fact]
    public void Process_AcceptsEchoNegotiationAndRecordsIt()
    {
        var parser = new TelnetFrameParser();
        var buffer = new byte[] { 255, 251, 1 };

        var result = parser.Process(buffer);

        var negotiation = Assert.Single(result.Negotiations);
        Assert.Equal(251, negotiation.Command);
        Assert.Equal(1, negotiation.OptionCode);
        Assert.Single(result.NegotiationResponses);
        Assert.Equal(new byte[] { 255, 253, 1 }, result.NegotiationResponses[0]);
        Assert.True(parser.OptionState.GetRemoteOptionState(TelnetOptions.Echo));
    }

    [Fact]
    public void Process_ConfirmsEchoDisableNegotiation()
    {
        var parser = new TelnetFrameParser();
        var buffer = new byte[] { 255, 252, 1 };

        var result = parser.Process(buffer);

        var negotiation = Assert.Single(result.Negotiations);
        Assert.Equal(252, negotiation.Command);
        Assert.Equal(1, negotiation.OptionCode);
        Assert.Single(result.NegotiationResponses);
        Assert.Equal(new byte[] { 255, 254, 1 }, result.NegotiationResponses[0]);
        Assert.False(parser.OptionState.GetRemoteOptionState(TelnetOptions.Echo));
    }

    [Fact]
    public void Process_RefusesClientSideEchoRequests()
    {
        var parser = new TelnetFrameParser();
        var buffer = new byte[] { 255, 253, 1 };

        var result = parser.Process(buffer);

        var negotiation = Assert.Single(result.Negotiations);
        Assert.Equal(253, negotiation.Command);
        Assert.Equal(1, negotiation.OptionCode);
        Assert.Single(result.NegotiationResponses);
        Assert.Equal(new byte[] { 255, 252, 1 }, result.NegotiationResponses[0]);
        Assert.False(parser.OptionState.GetLocalOptionState(TelnetOptions.Echo));
    }

    [Fact]
    public void Process_DoesNotRepeatAcceptedNegotiationResponses()
    {
        var parser = new TelnetFrameParser();

        var first = parser.Process(new byte[] { 255, 251, 1 });
        var second = parser.Process(new byte[] { 255, 251, 1 });

        Assert.Single(first.NegotiationResponses);
        Assert.Empty(second.NegotiationResponses);
        Assert.True(parser.OptionState.GetRemoteOptionState(TelnetOptions.Echo));
    }

    [Fact]
    public void Process_TracksSuppressGoAheadInBothDirections()
    {
        var parser = new TelnetFrameParser();
        var buffer = new byte[] { 255, 251, 3, 255, 253, 3 };

        var result = parser.Process(buffer);

        Assert.Equal(2, result.Negotiations.Count);
        Assert.Equal(2, result.NegotiationResponses.Count);
        Assert.Equal(new byte[] { 255, 253, 3 }, result.NegotiationResponses[0]);
        Assert.Equal(new byte[] { 255, 251, 3 }, result.NegotiationResponses[1]);
        Assert.True(parser.OptionState.GetRemoteOptionState(TelnetOptions.SuppressGoAhead));
        Assert.True(parser.OptionState.GetLocalOptionState(TelnetOptions.SuppressGoAhead));
    }

    [Fact]
    public void Process_HidesLocalInputWhenRemoteEchoIsDisabledAfterNegotiation()
    {
        var parser = new TelnetFrameParser();

        parser.Process(new byte[] { 255, 251, 3, 255, 253, 3, 255, 251, 1 });
        parser.Process(new byte[] { 255, 252, 1 });

        Assert.True(parser.OptionState.ShouldHideLocalInput);
    }
}
