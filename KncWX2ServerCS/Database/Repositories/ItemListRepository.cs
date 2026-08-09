using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record ItemListEntry(
    long ItemUid,
    int ItemId,
    byte UsageType,
    int Quantity,
    int Endurance,
    short Period,
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

    public async Task<IReadOnlyList<ItemListEntry>> GetAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = """
            SELECT
                i.ItemUID,
                i.ItemID,
                CASE
                    WHEN q.ItemUID IS NOT NULL THEN 2
                    WHEN e.ItemUID IS NOT NULL THEN 1
                    ELSE 0
                END AS UsageType,
                COALESCE(q.Quantity, 0) AS Quantity,
                COALESCE(e.Endurance, -1) AS Endurance,
                COALESCE(p.Period, 0) AS Period,
                p.ExpirationDate AS EndDate,
                COALESCE(en.ELevel, 0) AS ELevel,
                COALESCE(s.Socket1, 0) AS Socket1,
                COALESCE(s.Socket2, 0) AS Socket2,
                COALESCE(s.Socket3, 0) AS Socket3,
                COALESCE(s.Socket4, 0) AS Socket4,
                i.InventoryCategory,
                i.SlotID
            FROM GItem AS i
            LEFT JOIN GItemQuantity AS q ON q.ItemUID = i.ItemUID
            LEFT JOIN GItemEndurance AS e ON e.ItemUID = i.ItemUID
            LEFT JOIN GItemEnchant AS en ON en.ItemUID = i.ItemUID
            LEFT JOIN GItemPeriod AS p ON p.ItemUID = i.ItemUID
            LEFT JOIN GItemSocket AS s ON s.ItemUID = i.ItemUID
            WHERE i.UnitUID = $unitUid
              AND i.Deleted = 0
            ORDER BY i.InventoryCategory;
            """;
        command.Parameters.AddWithValue("$unitUid", unitUid);

        var result = new List<ItemListEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(new(
                reader.GetInt64(0),
                reader.GetInt32(1),
                Convert.ToByte(reader.GetInt64(2)),
                reader.GetInt32(3),
                reader.GetInt32(4),
                Convert.ToInt16(reader.GetValue(5)),
                reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                Convert.ToByte(reader.GetValue(7)),
                Convert.ToInt16(reader.GetValue(8)),
                Convert.ToInt16(reader.GetValue(9)),
                Convert.ToInt16(reader.GetValue(10)),
                Convert.ToInt16(reader.GetValue(11)),
                Convert.ToByte(reader.GetValue(12)),
                Convert.ToByte(reader.GetValue(13))));
        }
        return result;
    }
}
