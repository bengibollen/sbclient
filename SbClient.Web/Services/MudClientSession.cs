using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using SbClient.Web.Models;
using SbClient.Web.Protocol;

namespace SbClient.Web.Services;

public sealed class MudClientSession : IAsyncDisposable
{
    private readonly IMudSideChannelDecoder _sideChannelDecoder;
    private readonly ILogger<MudClientSession> _logger;
    private readonly MudGatewayOptions _options;
    private readonly TelnetFrameParser _frameParser = new();
    private readonly StringBuilder _transcript = new();
    private readonly List<MudMediaItem> _mediaItems = [];
    private readonly List<string> _observedSideChannels = [];
    private readonly SemaphoreSlim _stateGate = new(1, 1);

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _readerCancellation;
    private Task? _readerTask;
    private IReadOnlyList<TerminalLine> _lines = [new TerminalLine([])];

    public MudClientSession(
        IMudSideChannelDecoder sideChannelDecoder,
        IOptions<MudGatewayOptions> options,
        ILogger<MudClientSession> logger)
    {
        _sideChannelDecoder = sideChannelDecoder;
        _logger = logger;
        _options = options.Value;
    }

    public event Action? Changed;

    public MudConnectionState ConnectionState { get; private set; } = MudConnectionState.Disconnected;

    public string StatusMessage { get; private set; } = "Disconnected";

    public string? LastError { get; private set; }

    public IReadOnlyList<TerminalLine> Lines => _lines;

    public IReadOnlyList<MudMediaItem> MediaItems => _mediaItems;

    public IReadOnlyList<string> ObservedSideChannels => _observedSideChannels;

    public bool IsConnected => ConnectionState == MudConnectionState.Connected;

    public bool IsConnecting => ConnectionState == MudConnectionState.Connecting;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("A host name is required.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        await _stateGate.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected || IsConnecting)
            {
                return;
            }

            ResetSessionState();
            ConnectionState = MudConnectionState.Connecting;
            StatusMessage = $"Connecting to {host}:{port}...";
            NotifyChanged();

            _readerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port, _readerCancellation.Token);

            _stream = _tcpClient.GetStream();
            ConnectionState = MudConnectionState.Connected;
            StatusMessage = $"Connected to {host}:{port}";
            NotifyChanged();

            _readerTask = Task.Run(() => ReadLoopAsync(_readerCancellation.Token));
        }
        catch (SocketException exception)
        {
            await CleanupConnectionAsync();
            SetErrorState($"Unable to connect to {host}:{port}", exception.Message);
        }
        catch (IOException exception)
        {
            await CleanupConnectionAsync();
            SetErrorState($"Unable to connect to {host}:{port}", exception.Message);
        }
        finally
        {
            _stateGate.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _stateGate.WaitAsync();
        try
        {
            await DisconnectCoreAsync("Disconnected");
        }
        finally
        {
            _stateGate.Release();
        }
    }

    public async Task SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream is null)
        {
            throw new InvalidOperationException("Cannot send a command while disconnected.");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        AppendTranscript($"> {command}\n");

        try
        {
            var payload = Encoding.UTF8.GetBytes($"{command}\n");
            await _stream.WriteAsync(payload, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Failed to send a command to the MUD.");
            await CleanupConnectionAsync();
            SetErrorState("The telnet connection ended unexpectedly.", exception.Message);
        }
        catch (SocketException exception)
        {
            _logger.LogWarning(exception, "Failed to send a command to the MUD.");
            await CleanupConnectionAsync();
            SetErrorState("The telnet connection ended unexpectedly.", exception.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _stateGate.Dispose();
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (_stream is not null)
            {
                var read = await _stream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    await _stateGate.WaitAsync(cancellationToken);
                    try
                    {
                        await CleanupConnectionAsync(awaitReaderTask: false);
                        ConnectionState = MudConnectionState.Disconnected;
                        StatusMessage = "Connection closed by server";
                        LastError = null;
                        NotifyChanged();
                    }
                    finally
                    {
                        _stateGate.Release();
                    }

                    return;
                }

                var result = _frameParser.Process(buffer.AsSpan(0, read));
                if (!string.IsNullOrEmpty(result.Text))
                {
                    AppendTranscript(result.Text);
                }

                foreach (var response in result.NegotiationResponses)
                {
                    await _stream.WriteAsync(response, cancellationToken);
                }

                foreach (var subnegotiation in result.Subnegotiations)
                {
                    var observation = $"Option {subnegotiation.OptionCode} ({subnegotiation.Payload.Length} bytes)";
                    if (!_observedSideChannels.Contains(observation))
                    {
                        _observedSideChannels.Add(observation);
                    }

                    foreach (var mediaItem in _sideChannelDecoder.Decode(subnegotiation))
                    {
                        _mediaItems.Add(mediaItem);
                    }
                }

                NotifyChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "The telnet connection ended with an I/O error.");
            await CleanupConnectionAsync(awaitReaderTask: false);
            SetErrorState("The telnet connection ended unexpectedly.", exception.Message);
        }
        catch (SocketException exception)
        {
            _logger.LogWarning(exception, "The telnet connection ended with a socket error.");
            await CleanupConnectionAsync(awaitReaderTask: false);
            SetErrorState("The telnet connection ended unexpectedly.", exception.Message);
        }
    }

    private async Task DisconnectCoreAsync(string statusMessage)
    {
        await CleanupConnectionAsync();

        if (ConnectionState != MudConnectionState.Error)
        {
            ConnectionState = MudConnectionState.Disconnected;
            StatusMessage = statusMessage;
            LastError = null;
        }

        NotifyChanged();
    }

    private void AppendTranscript(string text)
    {
        _transcript.Append(text);
        _lines = AnsiTranscriptParser.Parse(_transcript.ToString(), _options.ScrollbackLineLimit);
    }

    private void ResetSessionState()
    {
        _frameParser.Reset();
        _transcript.Clear();
        _mediaItems.Clear();
        _observedSideChannels.Clear();
        _lines = [new TerminalLine([])];
        LastError = null;
    }

    private void SetErrorState(string statusMessage, string errorMessage)
    {
        ConnectionState = MudConnectionState.Error;
        StatusMessage = statusMessage;
        LastError = errorMessage;
        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();

    private async Task CleanupConnectionAsync(bool awaitReaderTask = true)
    {
        _readerCancellation?.Cancel();

        if (_stream is not null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        _tcpClient?.Dispose();
        _tcpClient = null;

        if (_readerTask is not null && awaitReaderTask)
        {
            try
            {
                await _readerTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _readerTask = null;
        _readerCancellation?.Dispose();
        _readerCancellation = null;
    }
}
