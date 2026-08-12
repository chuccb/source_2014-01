using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record PetSupplyResult(
    int ItemType,
    int ItemId,
    int Factor,
    string RegDate,
    string StartDate,
    string EndDate,
    long? ItemUid);

public sealed class PetSupplyRepository
{
    private readonly SqliteDatabase _database;

    public PetSupplyRepository(SqliteDatabase database) => _database = database;

    public async Task<IReadOnlyList<PetSupplyResult>> SupplyAsync(
        int loginUid,
        int petId,
        byte promotion,
        CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        var owner = await ScalarAsync(
            "SELECT Login FROM Users WHERE LoginUID=$loginUid;",
            cancellationToken,
            ("$loginUid", loginUid))
            .ConfigureAwait(false);

        var ownerLogin = owner?.ToString();
        if (string.IsNullOrEmpty(ownerLogin))
            return Array.Empty<PetSupplyResult>();

        var now = DateTime.Now;
        var rows = new List<WorkItem>();

        await using (var command = _database.Connection.CreateCommand())
        {
            command.CommandText = "SELECT ItemType,ItemID,Factor FROM GPetItem WHERE PetID=$petId AND Promotion=$promotion ORDER BY rowid;";
            command.Parameters.AddWithValue("$petId", petId);
            command.Parameters.AddWithValue("$promotion", promotion);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var type = reader.GetInt32(0);
                var id = reader.GetInt32(1);
                var factor = reader.GetInt32(2);
                var end = type == 0 && factor > 0 ? now.AddDays(factor) : now;
                rows.Add(new(type, id, factor, now, now, end, null));
            }
        }

        if (rows.Count == 0)
            return Array.Empty<PetSupplyResult>();

        await using var tx = (SqliteTransaction)await _database.Connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];

                if (item.Type == 0 && item.Factor > 0)
                {
                    var itemUid = await FindScalarAsync(
                        tx,
                        "SELECT ItemUID FROM GoodsObjectList WHERE OwnerLogin=$owner AND Period>0 AND ItemID=$itemId ORDER BY ItemUID LIMIT 1;",
                        cancellationToken,
                        ("$owner", ownerLogin),
                        ("$itemId", item.Id))
                        .ConfigureAwait(false);

                    rows[i] = item with { ItemUid = itemUid };
                }
                else if (item.Type == 1)
                {
                    var itemUid = await FindScalarAsync(
                        tx,
                        "SELECT DurationItemUID FROM DurationItemObjectList WHERE OwnerLogin=$owner AND GoodsID=$itemId ORDER BY DurationItemUID LIMIT 1;",
                        cancellationToken,
                        ("$owner", ownerLogin),
                        ("$itemId", item.Id))
                        .ConfigureAwait(false);

                    rows[i] = item with { ItemUid = itemUid };
                }
            }

            foreach (var item in rows)
            {
                if (item.ItemUid is not null && item.Type == 0 && item.Factor > 0)
                {
                    await ExecuteAsync(
                        tx,
                        "UPDATE GoodsObjectList SET EndDate=datetime(EndDate, '+' || $factor || ' days'), Period=Period+$factor WHERE ItemUID=$uid AND OwnerLogin=$owner AND Period>0 AND ItemID=$itemId;",
                        cancellationToken,
                        ("$factor", item.Factor),
                        ("$uid", item.ItemUid.Value),
                        ("$owner", ownerLogin),
                        ("$itemId", item.Id))
                        .ConfigureAwait(false);
                }
                else if (item.ItemUid is not null && item.Type == 1)
                {
                    await ExecuteAsync(
                        tx,
                        "UPDATE DurationItemObjectList SET Duration=Duration+$factor WHERE DurationItemUID=$uid AND OwnerLogin=$owner AND GoodsID=$itemId;",
                        cancellationToken,
                        ("$factor", item.Factor),
                        ("$uid", item.ItemUid.Value),
                        ("$owner", ownerLogin),
                        ("$itemId", item.Id))
                        .ConfigureAwait(false);
                }
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                if (item.ItemUid is not null)
                    continue;

                long itemUid;

                if (item.Type == 0)
                {
                    await ExecuteAsync(
                        tx,
                        "INSERT INTO GoodsObjectList(OwnerLogin,BuyerLogin,ItemID,RegDate,StartDate,EndDate,Period) VALUES($owner,'__PetSystem__',$itemId,$now,$start,$end,$factor);",
                        cancellationToken,
                        ("$owner", ownerLogin),
                        ("$itemId", item.Id),
                        ("$now", Format(item.RegDate)),
                        ("$start", Format(item.StartDate)),
                        ("$end", Format(item.EndDate)),
                        ("$factor", item.Factor))
                        .ConfigureAwait(false);

                    itemUid = await GetLastInsertIdAsync(tx, cancellationToken).ConfigureAwait(false);
                }
                else if (item.Type == 1)
                {
                    await ExecuteAsync(
                        tx,
                        "INSERT INTO DurationItemObjectList(OwnerLogin,BuyerLogin,GoodsID,Duration,RegDate) VALUES($owner,'__PetSystem__',$itemId,$factor,$now);",
                        cancellationToken,
                        ("$owner", ownerLogin),
                        ("$itemId", item.Id),
                        ("$factor", item.Factor),
                        ("$now", Format(item.RegDate)))
                        .ConfigureAwait(false);

                    itemUid = await GetLastInsertIdAsync(tx, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    continue;
                }

                rows[i] = item with { ItemUid = itemUid };
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

            return rows
                .Select(item => new PetSupplyResult(
                    item.Type,
                    item.Id,
                    item.Factor,
                    Format(item.RegDate),
                    Format(item.StartDate),
                    Format(item.EndDate),
                    item.ItemUid))
                .ToArray();
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private sealed record WorkItem(
        int Type,
        int Id,
        int Factor,
        DateTime RegDate,
        DateTime StartDate,
        DateTime EndDate,
        long? ItemUid);

    private async Task<object?> ScalarAsync(
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object?> ScalarTxAsync(
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

        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<long?> FindScalarAsync(
        SqliteTransaction tx,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        var value = await ScalarTxAsync(tx, sql, cancellationToken, parameters).ConfigureAwait(false);
        return value is null ? null : Convert.ToInt64(value);
    }

    private static async Task<long> GetLastInsertIdAsync(
        SqliteTransaction tx,
        CancellationToken cancellationToken)
    {
        var value = await ScalarTxAsync(tx, "SELECT last_insert_rowid();", cancellationToken)
            .ConfigureAwait(false);
        return Convert.ToInt64(value);
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

    private static string Format(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");
}
