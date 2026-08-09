using System.Net;
using System.Net.Sockets;
using KncWX2Server.Core.Logging;
using Serilog;

namespace KncWX2Server.Core.Network;

/// <summary>
/// Default implementation of ISession for TCP/IP connections.
/// Handles socket communication and message framing.
/// </summary>
public sealed class TcpSession : ISession
{
    private readonly Socket _socket;
    private readonly ILogger _logger;
    private readonly byte[] _receiveBuffer;
    private readonly CancellationTokenSource _closeCts;
    private bool _disposed;
    private Task? _receiveTask;

    public string SessionId { get; }
    public string RemoteAddress { get; }
    public long? UserId { get; set; }
    public bool IsConnected => _socket?.Connected ?? false;
    public DateTime CreatedAt { get; }
    public DateTime LastActivityAt { get; private set; }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<SessionClosedEventArgs>? Closed;

    private const int BufferSize = 8192;

    public TcpSession(Socket socket, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(socket);

        _socket = socket;
        _logger = logger ?? Log.Logger;
        _receiveBuffer = new byte[BufferSize];
        _closeCts = new CancellationTokenSource();
        SessionId = Guid.NewGuid().ToString();
        RemoteAddress = socket.RemoteEndPoint?.ToString() ?? "Unknown";
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Starts listening for incoming messages.
    /// </summary>
    public void StartReceiving()
    {
        if (_receiveTask != null)
        {
            return; // Already receiving
        }

        _receiveTask = ReceiveLoopAsync(_closeCts.Token);
    }

    /// <summary>
    /// Sends a message to the client.
    /// </summary>
    public async Task SendMessageAsync(byte[] message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            LastActivityAt = DateTime.UtcNow;

            // Add message length header (4 bytes, big-endian)
            var lengthBytes = BitConverter.GetBytes(message.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            var frameData = new byte[lengthBytes.Length + message.Length];
            Buffer.BlockCopy(lengthBytes, 0, frameData, 0, lengthBytes.Length);
            Buffer.BlockCopy(message, 0, frameData, lengthBytes.Length, message.Length);

            await _socket.SendAsync(new ArraySegment<byte>(frameData), SocketFlags.None);
            _logger.LogInfoWithContext($"Sent {message.Length} bytes to {RemoteAddress}");
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(ex, $"Error sending message to {RemoteAddress}");
            await CloseAsync("Send error");
        }
    }

    /// <summary>
    /// Closes the session.
    /// </summary>
    public async Task CloseAsync(string reason = "Normal")
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _logger.LogInfoWithContext($"Closing session {SessionId} from {RemoteAddress}: {reason}");

        try
        {
            _closeCts.Cancel();
            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(ex, "Error closing socket");
        }

        Closed?.Invoke(this, new SessionClosedEventArgs(reason));
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                int bytesReceived = await _socket.ReceiveAsync(
                    new ArraySegment<byte>(_receiveBuffer),
                    SocketFlags.None);

                if (bytesReceived == 0)
                {
                    // Connection closed by client
                    await CloseAsync("Connection closed by remote");
                    break;
                }

                LastActivityAt = DateTime.UtcNow;
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(_receiveBuffer, bytesReceived));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when closing
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(ex, "Error in receive loop");
            await CloseAsync("Receive error");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _closeCts?.Dispose();
        _socket?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync("Disposed");
        Dispose();
    }
}
