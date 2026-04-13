using System.Text;

namespace SbClient.Web.Protocol;

public sealed class TelnetFrameParser
{
    private const byte Iac = 255;
    private const byte Dont = 254;
    private const byte Do = 253;
    private const byte Wont = 252;
    private const byte Will = 251;
    private const byte Sb = 250;
    private const byte Se = 240;

    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private readonly List<byte> _textBuffer = [];
    private readonly List<byte> _subnegotiationPayload = [];

    private ParserState _state = ParserState.Text;
    private byte _pendingCommand;
    private byte _subnegotiationOption;

    public TelnetProcessResult Process(ReadOnlySpan<byte> buffer)
    {
        var subnegotiations = new List<TelnetSubnegotiationMessage>();
        var responses = new List<byte[]>();

        foreach (var current in buffer)
        {
            switch (_state)
            {
                case ParserState.Text:
                    if (current == Iac)
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
                        case Iac:
                            _textBuffer.Add(Iac);
                            _state = ParserState.Text;
                            break;
                        case Do:
                        case Dont:
                        case Will:
                        case Wont:
                            _pendingCommand = current;
                            _state = ParserState.NegotiationOption;
                            break;
                        case Sb:
                            _state = ParserState.SubnegotiationOption;
                            break;
                        default:
                            _state = ParserState.Text;
                            break;
                    }
                    break;

                case ParserState.NegotiationOption:
                    if (_pendingCommand == Do)
                    {
                        responses.Add([Iac, Wont, current]);
                    }
                    else if (_pendingCommand == Will)
                    {
                        responses.Add([Iac, Dont, current]);
                    }

                    _state = ParserState.Text;
                    break;

                case ParserState.SubnegotiationOption:
                    _subnegotiationOption = current;
                    _subnegotiationPayload.Clear();
                    _state = ParserState.Subnegotiation;
                    break;

                case ParserState.Subnegotiation:
                    if (current == Iac)
                    {
                        _state = ParserState.SubnegotiationIac;
                    }
                    else
                    {
                        _subnegotiationPayload.Add(current);
                    }
                    break;

                case ParserState.SubnegotiationIac:
                    if (current == Se)
                    {
                        subnegotiations.Add(new TelnetSubnegotiationMessage(
                            _subnegotiationOption,
                            _subnegotiationPayload.ToArray(),
                            DateTimeOffset.UtcNow));
                        _subnegotiationPayload.Clear();
                        _state = ParserState.Text;
                    }
                    else if (current == Iac)
                    {
                        _subnegotiationPayload.Add(Iac);
                        _state = ParserState.Subnegotiation;
                    }
                    else
                    {
                        _state = ParserState.Subnegotiation;
                    }
                    break;
            }
        }

        return new TelnetProcessResult(DecodePendingText(), subnegotiations, responses);
    }

    public void Reset()
    {
        _decoder.Reset();
        _textBuffer.Clear();
        _subnegotiationPayload.Clear();
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
