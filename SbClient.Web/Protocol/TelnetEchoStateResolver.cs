namespace SbClient.Web.Protocol;

public sealed class TelnetEchoStateResolver
{
    public bool? IsRemoteEchoEnabled { get; private set; }

    public bool IsInputHidden => IsRemoteEchoEnabled == true;

    public void ApplyRemoteEchoState(bool isEchoEnabled)
    {
        IsRemoteEchoEnabled = isEchoEnabled;
    }

    public void ApplyNegotiations(IReadOnlyList<TelnetNegotiationMessage> negotiations)
    {
        foreach (var negotiation in negotiations)
        {
            if (negotiation.OptionCode != TelnetOptions.Echo)
            {
                continue;
            }

            switch (negotiation.Command)
            {
                case TelnetCommands.Will:
                    IsRemoteEchoEnabled = true;
                    break;
                case TelnetCommands.Wont:
                    IsRemoteEchoEnabled = false;
                    break;
                case TelnetCommands.Do:
                    IsRemoteEchoEnabled = false;
                    break;
                case TelnetCommands.Dont:
                    IsRemoteEchoEnabled = true;
                    break;
            }
        }
    }

    public void Reset()
    {
        IsRemoteEchoEnabled = null;
    }
}
