using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record ItemListRow(
    long ItemUid,
    int ItemId,
    byte UsageType,
    int Quantity,
    int Endurance,
    int Period,
    DateTime? EndDate,
    byte ELevel,
    short Socket1,
    short Socket2,
    short Socket3,
    short Socket4,
    byte InventoryCategory,
    byte SlotId);

public sealed class ItemListRepository
{
    private readonly SqliteDatabase _database;
    public ItemListRepository(SqliteDatabase database) => _database = database;

    // Direct relational equivalent of gup_get_item_list's staged @ItemList updates.
    // LEFT JOIN + COALESCE preserves the procedure's defaults when an optional row is absent.
    public async Task<IReadOnlyList<ItemListRow>> GetAsync(long unitUid, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = """
SELECT
    i.ItemUID,
    i.ItemID,
    CASE WHEN q.ItemUID IS NOT NULL THEN 2
         WHEN e.ItemUID IS NOT NULL THEN 1
         ELSE 0 END AS UsageType,
    COALESCE(q.Quantity, 0) AS Quantity,
    COALESCE(e.Endurance, -1) AS Endurance,
    COALESCE(p.Period, 0) AS Period,
    p.ExpirationDate AS EndDate,
    COALESCE(en.Elevel, 0) AS ELevel,
    COALESCE(s.Socket1, 0) AS Socket1,
    COALESCE(s.Socket2, 0) AS Socket2,
    COALESCE(s.Socket3, 0) AS Socket3,
    COALESCE(s.Socket4, 0) AS Socket4,
    i.InventoryCategory,
    i.SlotID
FROM GItem AS i
LEFT JOIN GItemQuantity AS q ON q.ItemUID = i.ItemUID
LEFT JOIN GItemEndurance AS e ON e.ItemUID = i.ItemUID
LEFT JOIN GItemPeriod AS p ON p.ItemUID = i.ItemUID
LEFT JOIN GItemEnchant AS en ON en.ItemUID = i.ItemUID
LEFT JOIN GItemSocket AS s ON s.ItemUID = i.ItemUID
WHERE i.UnitUID = $unitUid
  AND i.RegDate <> i.DelDate
ORDER BY i.InventoryCategory;
""";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        var result = new List<ItemListRow>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            result.Add(new(
                reader.GetInt64(0), reader.GetInt32(1), checked((byte)reader.GetInt32(2)),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5),
                reader.IsDBNull(6) ? null : ParseSmallDateTime(reader.GetString(6)),
                checked((byte)reader.GetInt32(7)), checked((short)reader.GetInt32(8)),
                checked((short)reader.GetInt32(9)), checked((short)reader.GetInt32(10)),
                checked((short)reader.GetInt32(11)), checked((byte)reader.GetInt32(12)),
                checked((byte)reader.GetInt32(13))));
        }
        return result;
    }

    private static DateTime ParseSmallDateTime(string value) => DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
