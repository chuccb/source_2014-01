namespace KncWX2Server.Database;

public static class SqliteSchema
{
    // SQLite has no SQL Server-style stored procedures. Business procedures from
    // the legacy database are migrated to C# repositories/services and explicit
    // transactions instead of emulating T-SQL procedure syntax.
    public const string Bootstrap = """
        PRAGMA foreign_keys = ON;
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous = NORMAL;
        PRAGMA busy_timeout = 5000;
        """;
}
