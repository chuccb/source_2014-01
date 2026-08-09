using Microsoft.Data.Sqlite;
using System.Globalization;
namespace KncWX2Server.Database.Repositories;
public sealed record ItemListRow(long ItemUid,int ItemId,byte UsageType,int Quantity,int Endurance,int Period,DateTime? EndDate,byte ELevel,short Socket1,short Socket2,short Socket3,short Socket4,byte InventoryCategory,byte SlotId);
public sealed class ItemListRepository
{
    private readonly SqliteDatabase _database;
    public ItemListRepository(SqliteDatabase database)=>_database=database;
    public Task<IReadOnlyList<ItemListRow>> GetAsync(long unitUid,CancellationToken ct=default)=>QueryAsync(unitUid,false,ct);
    public Task<IReadOnlyList<ItemListRow>> GetEquippedAsync(long unitUid,CancellationToken ct=default)=>QueryAsync(unitUid,true,ct);
    private async Task<IReadOnlyList<ItemListRow>> QueryAsync(long unitUid,bool equipped,CancellationToken ct)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var c=_database.Connection.CreateCommand();
        c.CommandText=$"""
SELECT i.ItemUID,i.ItemID,
 CASE WHEN e.ItemUID IS NOT NULL THEN 1 WHEN q.ItemUID IS NOT NULL THEN 2 ELSE 0 END,
 COALESCE(q.Quantity,0),COALESCE(e.Endurance,-1),COALESCE(p.Period,0),p.ExpirationDate,
 COALESCE(en.Elevel,0),COALESCE(s.Socket1,0),COALESCE(s.Socket2,0),COALESCE(s.Socket3,0),COALESCE(s.Socket4,0),i.InventoryCategory,i.SlotID
FROM GItem i
LEFT JOIN GItemQuantity q ON q.ItemUID=i.ItemUID
LEFT JOIN GItemEndurance e ON e.ItemUID=i.ItemUID
LEFT JOIN GItemPeriod p ON p.ItemUID=i.ItemUID
LEFT JOIN GItemEnchant en ON en.ItemUID=i.ItemUID
LEFT JOIN GItemSocket s ON s.ItemUID=i.ItemUID
WHERE i.UnitUID=$uid AND i.RegDate<>i.DelDate{(equipped ? " AND i.InventoryCategory=9" : "")}
ORDER BY i.InventoryCategory;
""";
        c.Parameters.AddWithValue("$uid",unitUid);var list=new List<ItemListRow>();
        await using var r=await c.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while(await r.ReadAsync(ct).ConfigureAwait(false))list.Add(new(r.GetInt64(0),r.GetInt32(1),checked((byte)r.GetInt32(2)),r.GetInt32(3),r.GetInt32(4),r.GetInt32(5),r.IsDBNull(6)?null:DateTime.Parse(r.GetString(6),CultureInfo.InvariantCulture),checked((byte)r.GetInt32(7)),checked((short)r.GetInt32(8)),checked((short)r.GetInt32(9)),checked((short)r.GetInt32(10)),checked((short)r.GetInt32(11)),checked((byte)r.GetInt32(12)),checked((byte)r.GetInt32(13))));
        return list;
    }
}
