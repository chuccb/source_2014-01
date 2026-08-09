namespace KncWX2Server.Core.Messaging;

/// <summary>
/// Base interface for handling messages.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Gets the message type this handler processes.
    /// </summary>
    int MessageType { get; }

    /// <summary>
    /// Gets the handler name for logging.
    /// </summary>
    string HandlerName { get; }

    /// <summary>
    /// Processes the incoming message.
    /// </summary>
    Task<bool> HandleAsync(object sender, MessageContext context);
}

/// <summary>
/// Represents the context of a message being processed.
/// </summary>
public sealed class MessageContext
{
    /// <summary>
    /// Gets the message type ID.
    /// </summary>
    public int MessageType { get; init; }

    /// <summary>
    /// Gets the message data.
    /// </summary>
    public byte[] Data { get; init; }

    /// <summary>
    /// Gets the length of the message data.
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Gets the sender information.
    /// </summary>
    public object? Sender { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was received.
    /// </summary>
    public DateTime ReceivedAt { get; init; }

    public MessageContext(int messageType, byte[] data, int length, object? sender = null)
    {
        MessageType = messageType;
        Data = data;
        Length = length;
        Sender = sender;
        ReceivedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Generic message handler base class.
/// </summary>
public abstract class MessageHandler<TMessage> : IMessageHandler where TMessage : class
{
    public abstract int MessageType { get; }
    public abstract string HandlerName { get; }

    public async Task<bool> HandleAsync(object sender, MessageContext context)
    {
        try
        {
            var message = DeserializeMessage(context.Data, context.Length);
            if (message == null)
            {
                return false;
            }

            return await ProcessMessageAsync(sender, message, context);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in message handler {HandlerName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deserializes the message data.
    /// Override this to implement custom deserialization.
    /// </summary>
    protected abstract TMessage? DeserializeMessage(byte[] data, int length);

    /// <summary>
    /// Processes the deserialized message.
    /// </summary>
    protected abstract Task<bool> ProcessMessageAsync(object sender, TMessage message, MessageContext context);
}
