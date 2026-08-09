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
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, cancellationToken);
        if (endpoint is null) return;
        _listener = new TcpListener(endpoint);
        _listener.Start();
        while (!linked.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(linked.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (linked.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) { break; }
            var id = Interlocked.Increment(ref _nextConnectionId);
            _clients[id] = client;
            _ = ProcessClientAsync(id, client, linked.Token);
        }
    }

    public Task StopAsync()
    {
        _stop.Cancel();
        _listener?.Stop();
        foreach (var pair in _clients)
        {
            try { pair.Value.Close(); pair.Value.Dispose(); } catch { }
        }
        _clients.Clear();
        return Task.CompletedTask;
    }

    private async Task ProcessClientAsync(long connectionId, TcpClient client, CancellationToken ct)
    {
        try
        {
            using (client)
            await using var stream = client.GetStream();
            var receive = new byte[8192];
            while (!ct.IsCancellationRequested)
            {
                var count = await stream.ReadAsync(receive.AsMemory(), ct).ConfigureAwait(false);
                if (count == 0) break;
                // Framing/authentication/encryption is intentionally not guessed here.
                // This is the transport boundary; the native NetLayer packet format
                // must be ported before arbitrary bytes can become KEvent instances.
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (IOException) { }
        catch (SocketException) { }
        finally { _clients.TryRemove(connectionId, out _); }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _stop.Dispose();
    }
}

public sealed class KEventDispatcher
{
    private readonly ConcurrentDictionary<ushort, Func<KEvent, CancellationToken, ValueTask>> _handlers = new();

    public void Register(ushort eventId, Func<KEvent, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryAdd(eventId, handler))
            throw new InvalidOperationException($"Event handler already registered: {eventId}.");
    }

    public bool Unregister(ushort eventId) => _handlers.TryRemove(eventId, out _);

    public ValueTask DispatchAsync(KEvent evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt);
        return _handlers.TryGetValue(evt.EventId, out var handler)
            ? handler(evt, ct)
            : ValueTask.CompletedTask;
    }
}
