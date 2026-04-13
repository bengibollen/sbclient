using SbClient.Web.Models;
using SbClient.Web.Protocol;

namespace SbClient.Web.Services;

public interface IMudSideChannelDecoder
{
    IReadOnlyList<MudMediaItem> Decode(TelnetSubnegotiationMessage message);
}
