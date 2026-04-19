using System.Text;

namespace SbClient.Web.Protocol;

public sealed class TelnetFrameParser
{
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private readonly List<byte> _textBuffer = [];
    private readonly List<byte> _subnegotiationPayload = [];
    private readonly TelnetOptionStateTracker _optionState = new();

    private ParserState _state = ParserState.Text;
    private byte _pendingCommand;
    private byte _subnegotiationOption;

    public TelnetOptionStateTracker OptionState => _optionState;

    public TelnetProcessResult Process(ReadOnlySpan<byte> buffer)
    {
        var subnegotiations = new List<TelnetSubnegotiationMessage>();
        var responses = new List<byte[]>();
        var negotiations = new List<TelnetNegotiationMessage>();

        foreach (var current in buffer)
        {
            switch (_state)
            {
                case ParserState.Text:
                    if (current == TelnetCommands.Iac)
                    {
                        _state = ParserState.Command;
                    }
                    else
                    {
                        _textBuffer.Add(current);
                    }
                    break;

                case ParserState.Command:
                    switch (current)
                    {
                        case TelnetCommands.Iac:
                            _textBuffer.Add(TelnetCommands.Iac);
                            _state = ParserState.Text;
                            break;
                        case TelnetCommands.Do:
                        case TelnetCommands.Dont:
                        case TelnetCommands.Will:
                        case TelnetCommands.Wont:
                            _pendingCommand = current;
                            _state = ParserState.NegotiationOption;
                            break;
                        case TelnetCommands.Sb:
                            _state = ParserState.SubnegotiationOption;
                            break;
                        default:
                            _state = ParserState.Text;
                            break;
                    }
                    break;

                case ParserState.NegotiationOption:
                    HandleNegotiation(responses, negotiations, current);
                    _state = ParserState.Text;
                    break;

                case ParserState.SubnegotiationOption:
                    _subnegotiationOption = current;
                    _subnegotiationPayload.Clear();
                    _state = ParserState.Subnegotiation;
                    break;

                case ParserState.Subnegotiation:
                    if (current == TelnetCommands.Iac)
                    {
                        _state = ParserState.SubnegotiationIac;
                    }
                    else
                    {
                        _subnegotiationPayload.Add(current);
                    }
                    break;

                case ParserState.SubnegotiationIac:
                    if (current == TelnetCommands.Se)
                    {
                        subnegotiations.Add(new TelnetSubnegotiationMessage(
                            _subnegotiationOption,
                            _subnegotiationPayload.ToArray(),
                            DateTimeOffset.UtcNow));
                        _subnegotiationPayload.Clear();
                        _state = ParserState.Text;
                    }
                    else if (current == TelnetCommands.Iac)
                    {
                        _subnegotiationPayload.Add(TelnetCommands.Iac);
                        _state = ParserState.Subnegotiation;
                    }
                    else
                    {
                        _state = ParserState.Subnegotiation;
                    }
                    break;
            }
        }

        return new TelnetProcessResult(DecodePendingText(), subnegotiations, responses, negotiations);
    }

    public void Reset()
    {
        _decoder.Reset();
        _textBuffer.Clear();
        _subnegotiationPayload.Clear();
        _optionState.Reset();
        _state = ParserState.Text;
        _pendingCommand = 0;
        _subnegotiationOption = 0;
    }

    private string DecodePendingText()
    {
        if (_textBuffer.Count == 0)
        {
            return string.Empty;
        }

        var bytes = _textBuffer.ToArray();
        var charCount = _decoder.GetCharCount(bytes, 0, bytes.Length, flush: false);
        var chars = new char[charCount];
        _decoder.GetChars(bytes, 0, bytes.Length, chars, 0, flush: false);
        _textBuffer.Clear();

        return new string(chars);
    }

    private void HandleNegotiation(
        List<byte[]> responses,
        List<TelnetNegotiationMessage> negotiations,
        byte optionCode)
    {
        negotiations.Add(new TelnetNegotiationMessage(_pendingCommand, optionCode, DateTimeOffset.UtcNow));

        switch (_pendingCommand)
        {
            case TelnetCommands.Will:
                HandleRemoteOptionRequest(responses, optionCode, IsSupportedRemoteOption(optionCode));
                return;
            case TelnetCommands.Wont:
                HandleRemoteOptionDisable(responses, optionCode);
                return;
            case TelnetCommands.Do:
                HandleLocalOptionRequest(responses, optionCode, IsSupportedLocalOption(optionCode));
                return;
            case TelnetCommands.Dont:
                HandleLocalOptionDisable(responses, optionCode);
                return;
        }
    }

    private void HandleRemoteOptionRequest(List<byte[]> responses, byte optionCode, bool accepted)
    {
        if (_optionState.GetRemoteOptionState(optionCode) != accepted)
        {
            responses.Add([TelnetCommands.Iac, accepted ? TelnetCommands.Do : TelnetCommands.Dont, optionCode]);
        }

        _optionState.SetRemoteOptionState(optionCode, accepted);
    }

    private void HandleRemoteOptionDisable(List<byte[]> responses, byte optionCode)
    {
        if (_optionState.GetRemoteOptionState(optionCode) != false)
        {
            responses.Add([TelnetCommands.Iac, TelnetCommands.Dont, optionCode]);
        }

        _optionState.SetRemoteOptionState(optionCode, false);
    }

    private void HandleLocalOptionRequest(List<byte[]> responses, byte optionCode, bool accepted)
    {
        if (_optionState.GetLocalOptionState(optionCode) != accepted)
        {
            responses.Add([TelnetCommands.Iac, accepted ? TelnetCommands.Will : TelnetCommands.Wont, optionCode]);
        }

        _optionState.SetLocalOptionState(optionCode, accepted);
    }

    private void HandleLocalOptionDisable(List<byte[]> responses, byte optionCode)
    {
        if (_optionState.GetLocalOptionState(optionCode) != false)
        {
            responses.Add([TelnetCommands.Iac, TelnetCommands.Wont, optionCode]);
        }

        _optionState.SetLocalOptionState(optionCode, false);
    }

    private static bool IsSupportedRemoteOption(byte optionCode)
        => optionCode is TelnetOptions.Echo or TelnetOptions.SuppressGoAhead;

    private static bool IsSupportedLocalOption(byte optionCode)
        => optionCode == TelnetOptions.SuppressGoAhead;

    private enum ParserState
    {
        Text,
        Command,
        NegotiationOption,
        SubnegotiationOption,
        Subnegotiation,
        SubnegotiationIac
    }
}
