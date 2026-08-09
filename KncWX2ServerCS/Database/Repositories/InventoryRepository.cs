using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class InventoryRepository
{
    private readonly SqliteDatabase _database;
    public InventoryRepository(SqliteDatabase database) => _database = database;

    public async Task<int> InsertSizeAsync(long unitUid, byte inventoryCategory, byte numSlot, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.Now;
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var rows = await ExecuteAsync(tx,
                "INSERT INTO GItemInventorySize(UnitUID,InventoryCategory,NumSlot,RegDate) VALUES($unitUid,$category,$numSlot,$regDate);",
                cancellationToken, ("$unitUid", unitUid), ("$category", inventoryCategory), ("$numSlot", numSlot), ("$regDate", Format(ToSmallDateTime(now))));
            if (rows != 1) return await RollbackAsync(tx, -1, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime value){var m=new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0,value.Kind);return value.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
