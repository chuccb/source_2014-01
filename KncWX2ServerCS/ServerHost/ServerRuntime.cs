using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using KncWX2Server.Protocol;

namespace KncWX2Server.ServerHost;

/// <summary>
/// Runtime shell for the five native server roles. It owns process lifetime,
/// listener lifetime and event dispatch; role-specific packet handlers are
/// attached explicitly instead of inventing a wire protocol here.
/// </summary>
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

    public async Task StartAsync(IPEndPoint? endpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(static state => ((CancellationTokenSource)state!).Cancel(), _stop);
        if (endpoint is null) return;
        _listener = new TcpListener(endpoint);
        _listener.Start();
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(_stop.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) { break; }
            var id = Interlocked.Increment(ref _nextConnectionId);
            _clients[id] = client;
            _ = ProcessClientAsync(id, client, _stop.Token);
        }
    }

    public async Task StopAsync()
    {
        _stop.Cancel();
        _listener?.Stop();
        foreach (var pair in _clients)
        {
            try { pair.Value.Close(); pair.Value.Dispose(); } catch { }
        }
        _clients.Clear();
        await Task.CompletedTask.ConfigureAwait(false);
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
                // Deliberately do not parse bytes here. Native NetLayer framing,
                // encryption/authentication and packet dispatch must be ported from
                // the corresponding source before bytes can be interpreted safely.
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
