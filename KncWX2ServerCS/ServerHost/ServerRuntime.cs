using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using KncWX2Server.Protocol;

namespace KncWX2Server.ServerHost;

/// <summary>Runtime lifecycle and transport shell shared by the five native server roles.</summary>
public sealed class ServerRuntime : IAsyncDisposable
{
    private readonly ServerRole _role;
    private readonly KEventDispatcher _dispatcher = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentDictionary<long, TcpClient> _clients = new();
    private TcpListener? _listener;
    private long _nextConnectionId;

    public ServerRuntime(ServerRole role) => _role = role;
    public ServerRole Role => _role;
    public int ClientCount => _clients.Count;
    public KEventDispatcher Dispatcher => _dispatcher;
    public CancellationToken ShutdownToken => _stop.Token;

    public async Task StartAsync(IPEndPoint? endpoint, CancellationToken cancellationToken = default)
    {
        if (endpoint is null)
            return;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, cancellationToken);
        _listener = new TcpListener(endpoint);
        _listener.Start();

        while (!linked.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            var connectionId = Interlocked.Increment(ref _nextConnectionId);
            _clients[connectionId] = client;
            _ = ProcessClientAsync(connectionId, client, linked.Token);
        }
    }

    public Task StopAsync()
    {
        _stop.Cancel();
        _listener?.Stop();

        foreach (var client in _clients.Values)
        {
            try
            {
                client.Close();
                client.Dispose();
            }
            catch
            {
                // Client cleanup must not prevent the remaining connections from closing.
            }
        }

        _clients.Clear();
        return Task.CompletedTask;
    }

    private async Task ProcessClientAsync(long connectionId, TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            await using var stream = client.GetStream();

            var receiveBuffer = new byte[8192];
            while (!cancellationToken.IsCancellationRequested)
            {
                var count = await stream.ReadAsync(receiveBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (count == 0)
                    break;

                // Packet framing/authentication/encryption remains intentionally
                // unimplemented until the native NetLayer packet format is ported.
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
        finally
        {
            _clients.TryRemove(connectionId, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _stop.Dispose();
    }
}

public sealed class KEventDispatcher
{
    private readonly ConcurrentDictionary<EventId, Func<KEvent, CancellationToken, ValueTask>> _handlers = new();

    public void Register(EventId eventId, Func<KEvent, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryAdd(eventId, handler))
            throw new InvalidOperationException($"Event handler already registered: {eventId}.");
    }

    public bool Unregister(EventId eventId) => _handlers.TryRemove(eventId, out _);

    public ValueTask DispatchAsync(KEvent evt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);
        return _handlers.TryGetValue(evt.Id, out var handler)
            ? handler(evt, cancellationToken)
            : ValueTask.CompletedTask;
    }
}
