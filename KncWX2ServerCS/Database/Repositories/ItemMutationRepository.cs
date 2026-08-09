using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class ItemMutationRepository
{
    private readonly SqliteDatabase _database;
    public ItemMutationRepository(SqliteDatabase database) => _database = database;

    public async Task<int> DeleteAsync(long itemUid, CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var now=DateTime.Now.AddMinutes(1); var affected=await ExecAsync(tx,"UPDATE GItem SET DelDate=$now WHERE ItemUID=$item;",ct,("$now",Format(now)),("$item",itemUid));
            if(affected!=1)return await RollbackAsync(tx,-2,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    public async Task<int> UpdateUsageAsync(long itemUid,int usageType,int param,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if(!await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GItem WHERE ItemUID=$item AND RegDate<>DelDate);",ct,("$item",itemUid)))return -1;
        if(usageType<1||usageType>2)return -2;
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if(usageType==2)
            {
                var q=await ScalarTxAsync(tx,"SELECT Quantity FROM GItemQuantity WHERE ItemUID=$item;",ct,("$item",itemUid));
                if(q is null||q==DBNull.Value)return await RollbackAsync(tx,-3,ct);
                if(Convert.ToInt32(q)+param<0)return await RollbackAsync(tx,-5,ct);
                if(await ExecAsync(tx,"UPDATE GItemQuantity SET Quantity=Quantity+$param WHERE ItemUID=$item;",ct,("$param",param),("$item",itemUid))!=1)return await RollbackAsync(tx,-3,ct);
            }
            else
            {
                if(await ExecAsync(tx,"UPDATE GItemEndurance SET Endurance=Endurance+$param WHERE ItemUID=$item;",ct,("$param",param),("$item",itemUid))!=1)return await RollbackAsync(tx,-4,ct);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    public async Task<int> UpdatePositionAsync(long itemUid,int inventoryCategory,int slotId,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if(!await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GItem WHERE ItemUID=$item AND RegDate<>DelDate);",ct,("$item",itemUid)))return -1;
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if(await ExecAsync(tx,"UPDATE GItem SET InventoryCategory=$category,SlotID=$slot WHERE ItemUID=$item;",ct,("$category",inventoryCategory),("$slot",slotId),("$item",itemUid))!=1)return await RollbackAsync(tx,-3,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    private async Task<bool> ExistsAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps)=>Convert.ToInt64(await ScalarAsync(sql,ct,ps).ConfigureAwait(false))!=0;
    private async Task<object?> ScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<object?> ScalarTxAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<int> RollbackAsync(SqliteTransaction tx,int result,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return result;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
