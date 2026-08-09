using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class ItemUpdateRepository
{
    private readonly SqliteDatabase _database;
    public ItemUpdateRepository(SqliteDatabase database) => _database = database;

    public async Task<int> UpdateAsync(long itemUid, int usageType, int param, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (!await ExistsAsync(itemUid, cancellationToken).ConfigureAwait(false)) return -1;
        if (usageType is < 1 or > 2) return -2;

        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (usageType == 2)
            {
                var quantity = await ScalarAsync(tx, "SELECT Quantity FROM GItemQuantity WHERE ItemUID = $itemUid LIMIT 1;", cancellationToken, ("$itemUid", itemUid));
                var current = quantity is null ? 0 : Convert.ToInt32(quantity);
                if (current + param < 0) return await RollbackAsync(tx, -5, cancellationToken).ConfigureAwait(false);
                if (await ExecuteAsync(tx, "UPDATE GItemQuantity SET Quantity = Quantity + $param WHERE ItemUID = $itemUid;", cancellationToken, ("$param", param), ("$itemUid", itemUid)) != 1)
                    return await RollbackAsync(tx, -3, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (await ExecuteAsync(tx, "UPDATE GItemEndurance SET Endurance = Endurance + $param WHERE ItemUID = $itemUid;", cancellationToken, ("$param", param), ("$itemUid", itemUid)) != 1)
                    return await RollbackAsync(tx, -4, cancellationToken).ConfigureAwait(false);
            }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<int> UpdatePositionAsync(long itemUid, int inventoryCategory, int slotId, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (!await ExistsAsync(itemUid, cancellationToken).ConfigureAwait(false)) return -1;
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (await ExecuteAsync(tx, "UPDATE GItem SET InventoryCategory = $category, SlotID = $slotId WHERE ItemUID = $itemUid;", cancellationToken, ("$category", inventoryCategory), ("$slotId", slotId), ("$itemUid", itemUid)) != 1)
                return await RollbackAsync(tx, -3, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<bool> ExistsAsync(long itemUid, CancellationToken ct)
    {
        await using var cmd = _database.Connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM GItem WHERE ItemUID = $itemUid AND Deleted = 0);";
        cmd.Parameters.AddWithValue("$itemUid", itemUid);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 0;
    }

    private static async Task<object?> ScalarAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var cmd = tx.Connection!.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql;
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result is DBNull ? null : result;
    }

    private static async Task<int> ExecuteAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var cmd = tx.Connection!.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql;
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<int> RollbackAsync(SqliteTransaction tx, int result, CancellationToken ct)
    { await tx.RollbackAsync(ct).ConfigureAwait(false); return result; }
}
