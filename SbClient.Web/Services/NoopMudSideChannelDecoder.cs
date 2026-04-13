using SbClient.Web.Models;
using SbClient.Web.Protocol;

namespace SbClient.Web.Services;

public sealed class NoopMudSideChannelDecoder : IMudSideChannelDecoder
{
    public IReadOnlyList<MudMediaItem> Decode(TelnetSubnegotiationMessage message) => [];
}
