using System.Text;
using SbClient.Web.Protocol;

namespace SbClient.Web.Tests;

public class TelnetFrameParserTests
{
    [Fact]
    public void Process_StripsNegotiationAndReturnsResponse()
    {
        var parser = new TelnetFrameParser();
        var buffer = new byte[] { 255, 253, 1, (byte)'O', (byte)'K' };

        var result = parser.Process(buffer);

        Assert.Equal("OK", result.Text);
        Assert.Single(result.NegotiationResponses);
        Assert.Equal(new byte[] { 255, 252, 1 }, result.NegotiationResponses[0]);
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
}
