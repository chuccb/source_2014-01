using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class UnitLoginRepository
{
    private readonly SqliteDatabase _database;
    public UnitLoginRepository(SqliteDatabase database) => _database = database;

    public async Task<int> UpdateAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var now = ToSmallDateTime(DateTime.Now);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var last = await ReadLastDateAsync(tx, unitUid, cancellationToken).ConfigureAwait(false);
            if (last is null || await ExistsDeletedAsync(tx, unitUid, cancellationToken).ConfigureAwait(false) == false)
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return -1;
            }
            if (last.Value.Date != now.Date)
                await ExecuteAsync(tx, "UPDATE GUnit SET PlayDayCnt = PlayDayCnt + 1 WHERE UnitUID = $unitUid;", cancellationToken, ("$unitUid", unitUid));
            if (await ExecuteAsync(tx, "UPDATE GUnit SET LoginCount = LoginCount + 1, LastDate = $lastDate WHERE UnitUID = $unitUid AND Deleted = 0;", cancellationToken, ("$lastDate", Format(now)), ("$unitUid", unitUid)) != 1)
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return -2;
            }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    private static async Task<DateTime?> ReadLastDateAsync(SqliteTransaction tx, long unitUid, CancellationToken ct)
    {
        await using var c = tx.Connection!.CreateCommand(); c.Transaction = tx;
        c.CommandText = "SELECT LastDate FROM GUnit WHERE UnitUID = $unitUid LIMIT 1;"; c.Parameters.AddWithValue("$unitUid", unitUid);
        var v = await c.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return v is null || v is DBNull ? null : DateTime.Parse(Convert.ToString(v)!);
    }
    private static async Task<bool> ExistsDeletedAsync(SqliteTransaction tx, long unitUid, CancellationToken ct)
    {
        await using var c = tx.Connection!.CreateCommand(); c.Transaction = tx;
        c.CommandText = "SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID = $unitUid AND Deleted = 0);"; c.Parameters.AddWithValue("$unitUid", unitUid);
        return Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 0;
    }
    private static async Task<int> ExecuteAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] p)
    {
        await using var c = tx.Connection!.CreateCommand(); c.Transaction = tx; c.CommandText = sql;
        foreach (var (n,v) in p) c.Parameters.AddWithValue(n,v); return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
    private static DateTime ToSmallDateTime(DateTime v) { var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind); return v.Second>=30?m.AddMinutes(1):m; }
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
