namespace SbClient.Web.Protocol;

public sealed class TelnetOptionStateTracker
{
    private readonly Dictionary<byte, bool> _remoteOptions = [];
    private readonly Dictionary<byte, bool> _localOptions = [];

    public bool? GetRemoteOptionState(byte optionCode)
        => _remoteOptions.TryGetValue(optionCode, out var enabled) ? enabled : null;

    public bool? GetLocalOptionState(byte optionCode)
        => _localOptions.TryGetValue(optionCode, out var enabled) ? enabled : null;

    public bool ShouldHideLocalInput
    {
        get
        {
            var remoteEchoEnabled = GetRemoteOptionState(TelnetOptions.Echo);
            var remoteSuppressGoAhead = GetRemoteOptionState(TelnetOptions.SuppressGoAhead) == true;
            var localSuppressGoAhead = GetLocalOptionState(TelnetOptions.SuppressGoAhead) == true;
            var hasNegotiatedInputControl = remoteEchoEnabled.HasValue || remoteSuppressGoAhead || localSuppressGoAhead;

            return hasNegotiatedInputControl && remoteEchoEnabled == false;
        }
    }

    public void SetRemoteOptionState(byte optionCode, bool enabled)
    {
        _remoteOptions[optionCode] = enabled;
    }

    public void SetLocalOptionState(byte optionCode, bool enabled)
    {
        _localOptions[optionCode] = enabled;
    }

    public void Reset()
    {
        _remoteOptions.Clear();
        _localOptions.Clear();
    }
}
