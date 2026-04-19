using System.Text;
using TelnetNegotiationCore.Builders;
using TelnetNegotiationCore.Interpreters;
using TelnetNegotiationCore.Protocols;

namespace SbClient.Web.Services;

public sealed class MudTelnetClient(ILogger<MudTelnetClient> logger) : IAsyncDisposable
{
    private static readonly Func<byte[], Encoding, TelnetInterpreter, ValueTask> IgnoreSubmittedText =
        static (_, _, _) => ValueTask.CompletedTask;

    private readonly ILogger<MudTelnetClient> _logger = logger;

    private Stream? _stream;
    private TelnetInterpreter? _interpreter;

    public event Action<bool>? RemoteEchoChanged;

    public bool? IsRemoteEchoEnabled { get; private set; }

    public async Task AttachAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        if (_interpreter is not null)
        {
            throw new InvalidOperationException("The telnet interpreter is already attached to a stream.");
        }

        _stream = stream;

        _interpreter = await new TelnetInterpreterBuilder()
            .UseMode(TelnetInterpreter.TelnetMode.Client)
            .UseLogger(_logger)
            .OnSubmit(IgnoreSubmittedText)
            .OnNegotiation(WriteToNetworkAsync)
            .AddPlugin(new EchoProtocol().OnEchoStateChanged(HandleEchoStateChangedAsync))
            .AddPlugin<SuppressGoAheadProtocol>()
            .BuildAsync();
    }

    public async Task InterpretAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_interpreter is null)
        {
            throw new InvalidOperationException("The telnet interpreter is not attached to a stream.");
        }

        await _interpreter.InterpretByteArrayAsync(buffer);
        await _interpreter.WaitForProcessingAsync(maxWaitMs: 250, additionalDelayMs: 0);
    }

    public async Task SendAsync(string command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_interpreter is null)
        {
            throw new InvalidOperationException("The telnet interpreter is not attached to a stream.");
        }

        var payload = Encoding.UTF8.GetBytes(command);
        await _interpreter.SendPromptAsync(payload);
    }

    public async ValueTask DisposeAsync()
    {
        if (_interpreter is not null)
        {
            await _interpreter.DisposeAsync();
            _interpreter = null;
        }

        _stream = null;
        IsRemoteEchoEnabled = null;
    }

    private async ValueTask HandleEchoStateChangedAsync(bool isEchoEnabled)
    {
        IsRemoteEchoEnabled = isEchoEnabled;
        RemoteEchoChanged?.Invoke(isEchoEnabled);
        await ValueTask.CompletedTask;
    }

    private async ValueTask WriteToNetworkAsync(ReadOnlyMemory<byte> data)
    {
        if (_stream is null)
        {
            return;
        }

        await _stream.WriteAsync(data);
        await _stream.FlushAsync();
    }
}
