using KncWX2Server.Core.Actor;
using KncWX2Server.Core.Authentication;
using KncWX2Server.Core.Data;
using KncWX2Server.Core.Database;
using KncWX2Server.Core.Logging;
using KncWX2Server.Core.Messaging;
using KncWX2Server.Core.Network;
using Serilog;

namespace KncWX2Server.Core.Server;

/// <summary>
/// Configuration for the game server.
/// </summary>
public sealed record ServerConfiguration
{
    public string BindAddress { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 9300;
    public string DatabaseConnectionString { get; init; } = "Server=localhost;Database=KncWX2;";
    public int MaxConnections { get; init; } = 1000;
    public float UpdateInterval { get; init; } = 0.016f; // ~60 FPS
    public string LogDirectory { get; init; } = "logs";
}

/// <summary>
/// Base game server class.
/// Orchestrates network, actors, authentication, and messaging.
/// </summary>
public abstract class GameServer : IAsyncDisposable
{
    protected readonly ServerConfiguration Configuration;
    protected readonly ILogger Logger;
    protected readonly NetworkServer NetworkServer;
    protected readonly GameSessionManager SessionManager;
    protected readonly ActorManager ActorManager;
    protected readonly MessageDispatcher MessageDispatcher;
    protected readonly AuthenticationService AuthService;
    protected readonly DatabaseConnectionFactory DatabaseFactory;

    private bool _disposed;
    private bool _running;

    public bool IsRunning => _running;
    public int ActivePlayerCount => SessionManager.GetActiveSessionCount();

    protected GameServer(ServerConfiguration config, IActorFactory actorFactory)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(actorFactory);

        Configuration = config;
        Logger = LoggerConfiguration.ConfigureLogger(config.LogDirectory);

        NetworkServer = new NetworkServer(config.BindAddress, config.Port, Logger);
        SessionManager = new GameSessionManager();
        ActorManager = new ActorManager(actorFactory);
        MessageDispatcher = new MessageDispatcher();
        AuthService = new AuthenticationService();
        DatabaseFactory = new DatabaseConnectionFactory(config.DatabaseConnectionString);

        SetupEventHandlers();
    }

    /// <summary>
    /// Starts the server.
    /// </summary>
    public virtual async Task StartAsync()
    {
        if (_running)
        {
            Logger.Warning("Server is already running");
            return;
        }

        try
        {
            Logger.Information("Starting game server...");

            // Test database connection
            if (!await DatabaseFactory.TestConnectionAsync())
            {
                Logger.Fatal("Failed to connect to database");
                throw new InvalidOperationException("Database connection failed");
            }

            Logger.Information("Database connection established");

            // Initialize server-wide data
            await OnInitializeAsync();

            // Start network server
            NetworkServer.Start();
            Logger.Information("Network server started on {Address}:{Port}",
                Configuration.BindAddress, Configuration.Port);

            // Start actor update loop
            ActorManager.StartUpdateLoop(Configuration.UpdateInterval);
            Logger.Information("Actor manager started with {UpdateInterval}ms interval",
                Configuration.UpdateInterval * 1000);

            _running = true;
            Logger.Information("Game server started successfully");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to start game server");
            throw;
        }
    }

    /// <summary>
    /// Stops the server gracefully.
    /// </summary>
    public virtual async Task StopAsync()
    {
        if (!_running)
        {
            return;
        }

        try
        {
            _running = false;
            Logger.Information("Stopping game server...");

            // Stop accepting new connections
            await NetworkServer.StopAsync();
            Logger.Information("Network server stopped");

            // Stop actor updates
            await ActorManager.StopUpdateLoopAsync();
            Logger.Information("Actor manager stopped");

            // Close all sessions
            await SessionManager.CloseAllSessionsAsync("Server shutdown");
            Logger.Information("All sessions closed");

            // Cleanup
            await OnShutdownAsync();

            Logger.Information("Game server stopped");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error stopping game server");
        }
    }

    /// <summary>
    /// Called during server initialization.
    /// Override to load data and register handlers.
    /// </summary>
    protected virtual async Task OnInitializeAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called during server shutdown.
    /// Override to save data and cleanup.
    /// </summary>
    protected virtual async Task OnShutdownAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Registers a message handler.
    /// </summary>
    protected void RegisterMessageHandler(IMessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        MessageDispatcher.RegisterHandler(handler);
        Logger.Information("Registered message handler: {HandlerName} (Type {MessageType})",
            handler.HandlerName, handler.MessageType);
    }

    private void SetupEventHandlers()
    {
        NetworkServer.SessionAccepted += OnSessionAccepted;
    }

    private async void OnSessionAccepted(object? sender, SessionAcceptedEventArgs e)
    {
        try
        {
            var session = e.Session;
            var gameSession = new GameSession(session.SessionId, session);
            SessionManager.RegisterSession(gameSession);

            Logger.LogInfoWithContext($"New game session created: {gameSession.SessionId}");

            // Handle incoming messages
            session.MessageReceived += async (s, args) =>
            {
                try
                {
                    await OnClientMessageReceivedAsync(gameSession, args.Data, args.Length);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error processing client message");
                }
            };

            // Handle session close
            session.Closed += (s, args) =>
            {
                OnSessionClosed(gameSession, args.Reason);
            };

            await OnPlayerConnectedAsync(gameSession);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error handling session accepted");
        }
    }

    private async Task OnClientMessageReceivedAsync(GameSession gameSession, byte[] data, int length)
    {
        // TODO: Parse message header to determine message type
        // For now, just dispatch with a placeholder message type
        await MessageDispatcher.DispatchAsync(0, data, length, gameSession);
    }

    private void OnSessionClosed(GameSession gameSession, string reason)
    {
        Logger.LogInfoWithContext($"Session closed: {gameSession.SessionId}, reason: {reason}");
        SessionManager.UnregisterSession(gameSession.SessionId);
        OnPlayerDisconnected(gameSession);
    }

    /// <summary>
    /// Called when a player connects.
    /// Override to handle connection logic.
    /// </summary>
    protected virtual async Task OnPlayerConnectedAsync(GameSession gameSession)
    {
        Logger.Information("Player connected: {SessionId}", gameSession.SessionId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when a player disconnects.
    /// Override to handle disconnection logic.
    /// </summary>
    protected virtual void OnPlayerDisconnected(GameSession gameSession)
    {
        Logger.Information("Player disconnected: {SessionId}", gameSession.SessionId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await StopAsync();

        if (SessionManager is IDisposable sessionDisposable)
        {
            sessionDisposable.Dispose();
        }

        if (ActorManager is IAsyncDisposable actorAsyncDisposable)
        {
            await actorAsyncDisposable.DisposeAsync();
        }

        if (MessageDispatcher is IDisposable dispatcherDisposable)
        {
            dispatcherDisposable.Dispose();
        }

        if (NetworkServer is IAsyncDisposable networkAsyncDisposable)
        {
            await networkAsyncDisposable.DisposeAsync();
        }

        Log.CloseAndFlush();
        _disposed = true;
    }
}
