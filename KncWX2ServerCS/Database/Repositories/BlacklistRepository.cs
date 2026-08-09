using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record BlacklistEntry(long BlockUid, string Nickname);
public sealed record BlacklistInsertResult(int Code, string? Nickname);

public sealed class BlacklistRepository
{
    private readonly SqliteDatabase _database;
    public BlacklistRepository(SqliteDatabase database) => _database = database;

    public async Task<BlacklistInsertResult> InsertAsync(long myUid, long blockUid, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        var nickname = await ScalarAsync("SELECT Nickname FROM GUnitNickName WHERE UnitUID=$blockUid;", ct, ("$blockUid", blockUid));
        if (nickname is null) return new(-1, null);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var rows = await ExecuteAsync(tx, "INSERT INTO GBlacklist(MyUID,BlockUID) VALUES($myUid,$blockUid);", ct, ("$myUid", myUid), ("$blockUid", blockUid));
            if (rows != 1) return await RollbackAsync(tx, new BlacklistInsertResult(-2, nickname.ToString()), ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new(0, nickname.ToString());
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<int> DeleteAsync(long myUid, long blockUid, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if (!await ExistsAsync(myUid, blockUid, ct).ConfigureAwait(false)) return -1;
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var rows = await ExecuteAsync(tx, "DELETE FROM GBlacklist WHERE MyUID=$myUid AND BlockUID=$blockUid;", ct, ("$myUid", myUid), ("$blockUid", blockUid));
            if (rows != 1) return await RollbackAsync(tx, -2, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<IReadOnlyList<BlacklistEntry>> SelectAsync(long myUid, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var c = _database.Connection.CreateCommand();
        c.CommandText = "SELECT a.BlockUID,b.Nickname FROM GBlacklist a JOIN GUnitNickName b ON a.BlockUID=b.UnitUID WHERE a.MyUID=$myUid;";
        c.Parameters.AddWithValue("$myUid", myUid);
        var result = new List<BlacklistEntry>();
        await using var reader = await c.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false)) result.Add(new(reader.GetInt64(0), reader.GetString(1)));
        return result;
    }

    private async Task<bool> ExistsAsync(long myUid,long blockUid,CancellationToken ct){return Convert.ToInt64(await ScalarAsync("SELECT EXISTS(SELECT 1 FROM GBlacklist WHERE MyUID=$myUid AND BlockUID=$blockUid);",ct,("$myUid",myUid),("$blockUid",blockUid)).ConfigureAwait(false))!=0;}
    private async Task<object?> ScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
}
