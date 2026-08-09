using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class ItemDeletionRepository
{
    private readonly SqliteDatabase _database;
    public ItemDeletionRepository(SqliteDatabase database) => _database = database;

    public async Task<int> DeleteAsync(long itemUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var now = ToSmallDateTime(DateTime.Now.AddMinutes(1));
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var affected = await ExecuteAsync(tx, "UPDATE GItem SET DelDate = $delDate WHERE ItemUID = $itemUid;", cancellationToken, ("$delDate", Format(now)), ("$itemUid", itemUid));
            if (affected != 1) { await tx.RollbackAsync(cancellationToken).ConfigureAwait(false); return -2; }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params (string Name,object Value)[] p){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var(n,v)in p)c.Parameters.AddWithValue(n,v);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static DateTime ToSmallDateTime(DateTime v){var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind);return v.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
