using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class UnitLifecycleRepository
{
    private readonly SqliteDatabase _database;
    public UnitLifecycleRepository(SqliteDatabase database) => _database = database;

    public async Task<int> DeleteAsync(long unitUid, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        var nickname = await ScalarAsync("SELECT NickName FROM GUnitNickName WHERE UnitUID=$uid;", ct, ("$uid", unitUid));
        if (!await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID=$uid AND Deleted=0);", ct, ("$uid", unitUid))) return -1;
        if (nickname is null || nickname == DBNull.Value) return -2;

        var now = ToSmallDateTime(DateTime.Now.AddMinutes(1));
        var itemCount = Convert.ToInt64(await ScalarAsync("SELECT COUNT(*) FROM GItem WHERE UnitUID=$uid AND Deleted=0;", ct, ("$uid", unitUid)));
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if (await ExecAsync(tx, "UPDATE GUnit SET DelDate=$now WHERE UnitUID=$uid AND Deleted=0;", ct, ("$now", Format(now)), ("$uid", unitUid)) != 1) return await RollbackAsync(tx, -11, ct);
            await ExecAsync(tx, "UPDATE GTutor SET DelDate=$now WHERE (TeacherUID=$uid OR StudentUID=$uid) AND Deleted=0;", ct, ("$now", Format(now)), ("$uid", unitUid));
            if (await ExecAsync(tx, "UPDATE GUnitNickName SET NickName=NULL WHERE UnitUID=$uid;", ct, ("$uid", unitUid)) != 1) return await RollbackAsync(tx, -12, ct);
            if (await ExecAsync(tx, "INSERT INTO GDeletedNickNameHistory(NickName,UnitUID,Regdate) VALUES($nickname,$uid,$now);", ct, ("$nickname", nickname), ("$uid", unitUid), ("$now", Format(now))) != 1) return await RollbackAsync(tx, -14, ct);
            if (await ExecAsync(tx, "UPDATE GItem SET DelDate=$now WHERE UnitUID=$uid AND Deleted=0;", ct, ("$now", Format(now)), ("$uid", unitUid)) != itemCount) return await RollbackAsync(tx, -15, ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    private async Task<bool> ExistsAsync(string sql, CancellationToken ct, params (string Name, object Value)[] ps) => Convert.ToInt64(await ScalarAsync(sql, ct, ps).ConfigureAwait(false)) != 0;
    private async Task<object?> ScalarAsync(string sql, CancellationToken ct, params (string Name, object Value)[] ps)
    {
        await using var c = _database.Connection.CreateCommand(); c.CommandText = sql;
        foreach (var p in ps) c.Parameters.AddWithValue(p.Name, p.Value);
        return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);
    }
    private static async Task<int> ExecAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] ps)
    {
        await using var c = tx.Connection!.CreateCommand(); c.Transaction = tx; c.CommandText = sql;
        foreach (var p in ps) c.Parameters.AddWithValue(p.Name, p.Value);
        return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
    private static async Task<int> RollbackAsync(SqliteTransaction tx, int value, CancellationToken ct) { await tx.RollbackAsync(ct).ConfigureAwait(false); return value; }
    private static DateTime ToSmallDateTime(DateTime v) { var m = new DateTime(v.Year, v.Month, v.Day, v.Hour, v.Minute, 0, v.Kind); return v.Second >= 30 ? m.AddMinutes(1) : m; }
    private static string Format(DateTime v) => v.ToString("yyyy-MM-dd HH:mm");
}
