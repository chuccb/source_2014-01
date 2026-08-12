using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record InsertItemResult(int Code, long ItemUid, DateTime? EndDate);

public sealed class ItemRepository
{
    private readonly SqliteDatabase _database;
    public ItemRepository(SqliteDatabase database) => _database = database;

    public async Task<InsertItemResult> InsertAsync(long unitUid,int itemId,byte periodType,short quantity,int endurance,int period,short eLevel=0,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if(!await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID=$uid AND Deleted=0);",ct,("$uid",unitUid)))return new(-1,0,null);
        var now=ToSmallDateTime(DateTime.Now); DateTime? end=null;
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try{
            var itemUid=Convert.ToInt64(await ScalarTxAsync(tx,"INSERT INTO GItem(UnitUID,ItemID,InventoryCategory,SlotID,RegDate,DelDate) VALUES($uid,$item,0,0,$now,$now); SELECT last_insert_rowid();",ct,("$uid",unitUid),("$item",itemId),("$now",Format(now))));
            if(itemUid<=0)return await RollbackAsync(tx,new(-11,0,end),ct);
            if(periodType==0&&period>0){end=now.AddDays(period);if(await ExecAsync(tx,"INSERT INTO GItemPeriod(ItemUID,Period,ExpirationDate) VALUES($itemUid,$period,$end);",ct,("$itemUid",itemUid),("$period",period),("$end",Format(end.Value)))!=1)return await RollbackAsync(tx,new(-12,itemUid,end),ct);}
            if(periodType==1&&await ExecAsync(tx,"INSERT INTO GItemEndurance(ItemUID,Endurance) VALUES($itemUid,$value);",ct,("$itemUid",itemUid),("$value",endurance))!=1)return await RollbackAsync(tx,new(-13,itemUid,end),ct);
            if(periodType==2&&await ExecAsync(tx,"INSERT INTO GItemQuantity(ItemUID,Quantity) VALUES($itemUid,$value);",ct,("$itemUid",itemUid),("$value",quantity))!=1)return await RollbackAsync(tx,new(-12,itemUid,end),ct);
            if(eLevel>0&&await ExecAsync(tx,"INSERT INTO GItemEnchant(ItemUID,Elevel) VALUES($itemUid,$level);",ct,("$itemUid",itemUid),("$level",eLevel))!=1)return await RollbackAsync(tx,new(-14,itemUid,end),ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return new(0,itemUid,end);
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    public async Task<IReadOnlyList<ItemRow>> GetInventoryAsync(long unitUid,CancellationToken ct=default){await _database.OpenAsync(ct).ConfigureAwait(false);await using var c=_database.Connection.CreateCommand();c.CommandText="""
SELECT i.ItemUID,i.ItemID,i.InventoryCategory,i.SlotID,
       p.Period,p.ExpirationDate,e.Endurance,q.Quantity,en.Elevel
FROM GItem i
LEFT JOIN GItemPeriod p ON p.ItemUID=i.ItemUID
LEFT JOIN GItemEndurance e ON e.ItemUID=i.ItemUID
LEFT JOIN GItemQuantity q ON q.ItemUID=i.ItemUID
LEFT JOIN GItemEnchant en ON en.ItemUID=i.ItemUID
WHERE i.UnitUID=$uid AND i.RegDate<>i.DelDate
ORDER BY i.ItemUID;
""";c.Parameters.AddWithValue("$uid",unitUid);var list=new List<ItemRow>();await using var r=await c.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false))list.Add(new(r.GetInt64(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3),r.IsDBNull(4)?null:r.GetInt32(4),r.IsDBNull(5)?null:r.GetString(5),r.IsDBNull(6)?null:r.GetInt32(6),r.IsDBNull(7)?null:r.GetInt32(7),r.IsDBNull(8)?null:r.GetInt32(8)));return list;}

    private async Task<bool> ExistsAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps)=>Convert.ToInt64(await ScalarAsync(sql,ct,ps).ConfigureAwait(false))!=0;
    private async Task<object?> ScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<object?> ScalarTxAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<InsertItemResult> RollbackAsync(SqliteTransaction tx,InsertItemResult result,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return result;}
    private static DateTime ToSmallDateTime(DateTime v){var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind);return v.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}

public sealed record ItemRow(long ItemUid,int ItemId,int InventoryCategory,int SlotId,int? Period,string? ExpirationDate,int? Endurance,int? Quantity,int? ELevel);
