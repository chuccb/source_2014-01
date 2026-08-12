using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record InsertItemResult(int Code, long ItemUid, DateTime? EndDate);

public sealed class ItemInsertionRepository
{
    private readonly SqliteDatabase _database;

    public ItemInsertionRepository(SqliteDatabase database) => _database = database;

    public async Task<InsertItemResult> InsertAsync(
        long unitUid,
        int itemId,
        byte periodType,
        short quantity,
        int endurance,
        int period,
        short enchantLevel = 0,
        CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!await UnitExistsAsync(unitUid, cancellationToken).ConfigureAwait(false))
            return new(-1, 0, null);

        var now = ToSmallDateTime(DateTime.Now);
        DateTime? endDate = null;

        await using var tx = (SqliteTransaction)await _database.Connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var itemUid = await InsertItemAsync(tx, unitUid, itemId, now, cancellationToken)
                .ConfigureAwait(false);

            if (itemUid <= 0)
                return await RollbackAsync(tx, new(-11, itemUid, endDate), cancellationToken).ConfigureAwait(false);

            if (periodType == 0 && period > 0)
            {
                endDate = now.AddDays(period);

                var rows = await ExecuteAsync(
                    tx,
                    "INSERT INTO GItemPeriod(ItemUID, Period, ExpirationDate) VALUES ($itemUid, $period, $expiration);",
                    cancellationToken,
                    ("$itemUid", itemUid),
                    ("$period", period),
                    ("$expiration", Format(endDate.Value)))
                    .ConfigureAwait(false);

                if (rows != 1)
                    return await RollbackAsync(tx, new(-12, itemUid, endDate), cancellationToken).ConfigureAwait(false);
            }

            if (periodType == 1)
            {
                var rows = await ExecuteAsync(
                    tx,
                    "INSERT INTO GItemEndurance(ItemUID, Endurance) VALUES ($itemUid, $endurance);",
                    cancellationToken,
                    ("$itemUid", itemUid),
                    ("$endurance", endurance))
                    .ConfigureAwait(false);

                if (rows != 1)
                    return await RollbackAsync(tx, new(-13, itemUid, endDate), cancellationToken).ConfigureAwait(false);
            }

            if (periodType == 2)
            {
                var rows = await ExecuteAsync(
                    tx,
                    "INSERT INTO GItemQuantity(ItemUID, Quantity) VALUES ($itemUid, $quantity);",
                    cancellationToken,
                    ("$itemUid", itemUid),
                    ("$quantity", quantity))
                    .ConfigureAwait(false);

                if (rows != 1)
                    return await RollbackAsync(tx, new(-12, itemUid, endDate), cancellationToken).ConfigureAwait(false);
            }

            if (enchantLevel > 0)
            {
                var rows = await ExecuteAsync(
                    tx,
                    "INSERT INTO GItemEnchant(ItemUID, ELevel) VALUES ($itemUid, $level);",
                    cancellationToken,
                    ("$itemUid", itemUid),
                    ("$level", enchantLevel))
                    .ConfigureAwait(false);

                if (rows != 1)
                    return await RollbackAsync(tx, new(-14, itemUid, endDate), cancellationToken).ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, itemUid, endDate);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<bool> UnitExistsAsync(long unitUid, CancellationToken cancellationToken)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID = $unitUid AND Deleted = 0);";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 0;
    }

    private static async Task<long> InsertItemAsync(
        SqliteTransaction tx,
        long unitUid,
        int itemId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = """
            INSERT INTO GItem(UnitUID, ItemID, InventoryCategory, SlotID, RegDate, DelDate)
            VALUES ($unitUid, $itemId, 0, 0, $now, $now);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$unitUid", unitUid);
        command.Parameters.AddWithValue("$itemId", itemId);
        command.Parameters.AddWithValue("$now", Format(now));
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task<int> ExecuteAsync(
        SqliteTransaction tx,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<InsertItemResult> RollbackAsync(
        SqliteTransaction tx,
        InsertItemResult result,
        CancellationToken cancellationToken)
    {
        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static DateTime ToSmallDateTime(DateTime value)
    {
        var minute = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
        return value.Second >= 30 ? minute.AddMinutes(1) : minute;
    }

    private static string Format(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");
}
