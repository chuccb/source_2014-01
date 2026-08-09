using KncWX2Server.ServerHost;

var options = ServerOptions.Parse(args);

if (options.Role is null)
{
    Console.Error.WriteLine("Usage: KncWX2Server.ServerHost --role <login|center|global|channel|game> [--config <path>]");
    return 2;
}

Console.WriteLine($"KncWX2Server C# 14 host: role={options.Role}, config={options.ConfigPath ?? "<default>"}");
Console.WriteLine("The role implementation is intentionally not stubbed: protocol/common conversion must land before a server is started.");
return 0;

namespace KncWX2Server.ServerHost;

public enum ServerRole
{
    Login,
    Center,
    Global,
    Channel,
    Game
}

public sealed record ServerOptions(ServerRole? Role, string? ConfigPath)
{
    public static ServerOptions Parse(string[] args)
    {
        ServerRole? role = null;
        string? config = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--role" when i + 1 < args.Length:
                    role = ParseRole(args[++i]);
                    break;
                case "--config" when i + 1 < args.Length:
                    config = args[++i];
                    break;
            }
        }

        return new(role, config);
    }

    private static ServerRole ParseRole(string value) => value.ToLowerInvariant() switch
    {
        "login" => ServerRole.Login,
        "center" => ServerRole.Center,
        "global" => ServerRole.Global,
        "channel" => ServerRole.Channel,
        "game" => ServerRole.Game,
        _ => throw new ArgumentException($"Unknown server role '{value}'.", nameof(value))
    };
}
