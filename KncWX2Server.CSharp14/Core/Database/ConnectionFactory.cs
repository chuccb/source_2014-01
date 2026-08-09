using Microsoft.EntityFrameworkCore;

namespace KncWX2Server.Core.Database;

/// <summary>
/// Factory for creating database connections and context instances.
/// Replaces ODBC connections with Entity Framework Core.
/// </summary>
public sealed class DatabaseConnectionFactory
{
    private readonly string _connectionString;
    private readonly DbContextOptionsBuilder<GameDbContext>? _optionsBuilder;

    public DatabaseConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates a new database context instance.
    /// </summary>
    public GameDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new GameDbContext(options);
    }

    /// <summary>
    /// Tests the database connection.
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var context = CreateContext();
            await context.Database.OpenConnectionAsync();
            await context.Database.CloseConnectionAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string GetConnectionString() => _connectionString;
}

/// <summary>
/// Entity Framework Core DbContext for the game database.
/// </summary>
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // DbSets will be added as migrations are implemented
    // public DbSet<Player> Players { get; set; } = null!;
    // public DbSet<Character> Characters { get; set; } = null!;
    // public DbSet<ExpData> ExpTable { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure entity mappings here
    }
}
