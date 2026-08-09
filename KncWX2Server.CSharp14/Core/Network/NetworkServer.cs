using System.Net;
using System.Net.Sockets;
using KncWX2Server.Core.Logging;
using Serilog;

namespace KncWX2Server.Core.Network;

/// <summary>
/// Handles incoming TCP connections and creates session objects.
/// </summary>
public sealed class NetworkServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly ILogger _logger;
    private readonly List<ISession> _sessions;
    private readonly ReaderWriterLockSlim _sessionsLock;
    private CancellationTokenSource? _acceptCts;
    private Task? _acceptTask;
    private bool _disposed;

    public IPAddress BindAddress { get; }
    public int Port { get; }
    public bool IsRunning => _acceptTask != null && !_acceptTask.IsCompleted;
    public int ActiveSessionCount => _sessions.Count;

    public event EventHandler<SessionAcceptedEventArgs>? SessionAccepted;

    public NetworkServer(string bindAddress, int port, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(bindAddress);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(port, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);

        BindAddress = IPAddress.Parse(bindAddress);
        Port = port;
        _logger = logger ?? Log.Logger;
        _listener = new TcpListener(BindAddress, Port);
        _sessions = [];
        _sessionsLock = new();
    }

    /// <summary>
    /// Starts the server and begins accepting connections.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            _logger.Warning("Server is already running");
            return;
        }

        try
        {
            _listener.Start();
            _acceptCts = new();
            _acceptTask = AcceptConnectionsAsync(_acceptCts.Token);
            _logger.Information("Network server started on {Address}:{Port}", BindAddress, Port);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start network server");
            throw;
        }
    }

    /// <summary>
    /// Stops the server and closes all connections.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        _logger.Information("Stopping network server");

        try
        {
            _acceptCts?.Cancel();
            if (_acceptTask != null)
            {
                try
                {
                    await _acceptTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _listener?.Stop();
            await CloseAllSessionsAsync("Server shutdown");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping network server");
        }
    }

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    public IReadOnlyList<ISession> GetActiveSessions()
    {
        _sessionsLock.EnterReadLock();
        try
        {
            return _sessions.Where(s => s.IsConnected).ToList();
        }
        finally
        {
            _sessionsLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Closes all sessions.
    /// </summary>
    public async Task CloseAllSessionsAsync(string reason = "Server closing")
    {
        _sessionsLock.EnterWriteLock();
        try
        {
            var sessionsCopy = _sessions.ToList();
            foreach (var session in sessionsCopy)
            {
                await session.CloseAsync(reason);
            }
            _sessions.Clear();
        }
        finally
        {
            _sessionsLock.ExitWriteLock();
        }
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var clientSocket = await _listener.AcceptSocketAsync(cancellationToken);
                if (clientSocket == null)
                {
                    continue;
                }

                try
                {
                    var remoteEndPoint = clientSocket.RemoteEndPoint?.ToString() ?? "Unknown";
                    _logger.Information("New connection from {RemoteAddress}", remoteEndPoint);

                    var session = new TcpSession(clientSocket, _logger);
                    session.Closed += (s, e) => OnSessionClosed(session);

                    _sessionsLock.EnterWriteLock();
                    try
                    {
                        _sessions.Add(session);
                    }
                    finally
                    {
                        _sessionsLock.ExitWriteLock();
                    }

                    session.StartReceiving();
                    SessionAccepted?.Invoke(this, new SessionAcceptedEventArgs(session));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error accepting connection");
                    clientSocket?.Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fatal error in accept loop");
        }
    }

    private void OnSessionClosed(ISession session)
    {
        _sessionsLock.EnterWriteLock();
        try
        {
            _sessions.Remove(session);
        }
        finally
        {
            _sessionsLock.ExitWriteLock();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await StopAsync();
        _acceptCts?.Dispose();
        _listener?.Stop();
        _listener?.Dispose();
        _sessionsLock?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Arguments for session accepted events.
/// </summary>
public sealed class SessionAcceptedEventArgs(ISession session) : EventArgs
{
    public ISession Session { get; } = session;
}
