using System.Net;
using KncWX2Server.ServerHost;

namespace KncWX2Server.ServerHost;

var options = ServerOptions.Parse(args);
if (options.Role is null)
{
    Console.Error.WriteLine(
        "Usage: KncWX2Server.ServerHost --role <login|center|global|channel|game> " +
        "[--listen <ip:port>] [--config <path>]");
    return 2;
}

await using var runtime = new ServerRuntime(options.Role.Value);
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    _ = runtime.StopAsync();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => _ = runtime.StopAsync();

Console.WriteLine(
    $"KncWX2Server C# 14: role={options.Role}, " +
    $"listen={options.Listen?.ToString() ?? "disabled"}, " +
    $"config={options.ConfigPath ?? "<default>"}");

if (options.Listen is null)
{
    Console.WriteLine("Runtime started without a listener. Supply --listen to accept TCP connections.");
    await Task.Delay(Timeout.Infinite, runtime.ShutdownToken).ConfigureAwait(false);
    return 0;
}

await runtime.StartAsync(options.Listen).ConfigureAwait(false);
return 0;

public enum ServerRole
{
    Login,
    Center,
    Global,
    Channel,
    Game,
}

public sealed record ServerOptions(ServerRole? Role, IPEndPoint? Listen, string? ConfigPath)
{
    public static ServerOptions Parse(string[] args)
    {
        ServerRole? role = null;
        IPEndPoint? listen = null;
        string? config = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--role" when i + 1 < args.Length:
                    role = ParseRole(args[++i]);
                    break;

                case "--listen" when i + 1 < args.Length:
                    listen = ParseEndpoint(args[++i]);
                    break;

                case "--config" when i + 1 < args.Length:
                    config = args[++i];
                    break;
            }
        }

        return new(role, listen, config);
    }

    private static ServerRole ParseRole(string value) => value.ToLowerInvariant() switch
    {
        "login" => ServerRole.Login,
        "center" => ServerRole.Center,
        "global" => ServerRole.Global,
        "channel" => ServerRole.Channel,
        "game" => ServerRole.Game,
        _ => throw new ArgumentException($"Unknown server role '{value}'.", nameof(value)),
    };

    private static IPEndPoint ParseEndpoint(string value)
    {
        var split = value.LastIndexOf(':');
        if (split <= 0 || split == value.Length - 1)
            throw new ArgumentException($"Invalid listen endpoint '{value}'. Expected ip:port.", nameof(value));

        if (!int.TryParse(value[(split + 1)..], out var port) || port is < 1 or > 65535)
            throw new ArgumentException($"Invalid listen port in '{value}'.", nameof(value));

        var host = value[..split];
        var address = IPAddress.TryParse(host, out var parsed)
            ? parsed
            : Dns.GetHostAddresses(host).First();

        return new(address, port);
    }
}
