using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record PetSupplyResult(int ItemType, int ItemId, int Factor, string RegDate, string StartDate, string EndDate, long? ItemUid);

public sealed class PetSupplyRepository
{
    private readonly SqliteDatabase _database;
    public PetSupplyRepository(SqliteDatabase database) => _database = database;

    public async Task<IReadOnlyList<PetSupplyResult>> SupplyAsync(int loginUid, int petId, byte promotion, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        var owner = await ScalarAsync("SELECT Login FROM Users WHERE LoginUID=$loginUid;", ct, ("$loginUid", loginUid));
        var ownerLogin = owner?.ToString();
        if (string.IsNullOrEmpty(ownerLogin)) return Array.Empty<PetSupplyResult>();

        var now = DateTime.Now;
        var rows = new List<WorkItem>();
        await using (var command = _database.Connection.CreateCommand())
        {
            command.CommandText = "SELECT ItemType,ItemID,Factor FROM GPetItem WHERE PetID=$petId AND Promotion=$promotion ORDER BY rowid;";
            command.Parameters.AddWithValue("$petId", petId);
            command.Parameters.AddWithValue("$promotion", promotion);
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var type = reader.GetInt32(0);
                var id = reader.GetInt32(1);
                var factor = reader.GetInt32(2);
                var end = type == 0 && factor > 0 ? now.AddDays(factor) : now;
                rows.Add(new(type, id, factor, now, now, end, null));
            }
        }
        if (rows.Count == 0) return Array.Empty<PetSupplyResult>();

        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                if (item.Type == 0 && item.Factor > 0)
                    rows[i] = item with { ItemUid = await FindScalarAsync(tx, "SELECT ItemUID FROM GoodsObjectList WHERE OwnerLogin=$owner AND Period>0 AND ItemID=$itemId ORDER BY ItemUID LIMIT 1;", ct, ("$owner", ownerLogin), ("$itemId", item.Id)) };
                else if (item.Type == 1)
                    rows[i] = item with { ItemUid = await FindScalarAsync(tx, "SELECT DurationItemUID FROM DurationItemObjectList WHERE OwnerLogin=$owner AND GoodsID=$itemId ORDER BY DurationItemUID LIMIT 1;", ct, ("$owner", ownerLogin), ("$itemId", item.Id)) };
            }

            foreach (var item in rows)
            {
                if (item.ItemUid is not null && item.Type == 0 && item.Factor > 0)
                {
                    await ExecuteAsync(tx, "UPDATE GoodsObjectList SET EndDate=datetime(EndDate, '+' || $factor || ' days'), Period=Period+$factor WHERE ItemUID=$uid AND OwnerLogin=$owner AND Period>0 AND ItemID=$itemId;", ct,
                        ("$factor", item.Factor), ("$uid", item.ItemUid.Value), ("$owner", ownerLogin), ("$itemId", item.Id));
                }
                else if (item.ItemUid is not null && item.Type == 1)
                {
                    await ExecuteAsync(tx, "UPDATE DurationItemObjectList SET Duration=Duration+$factor WHERE DurationItemUID=$uid AND OwnerLogin=$owner AND GoodsID=$itemId;", ct,
                        ("$factor", item.Factor), ("$uid", item.ItemUid.Value), ("$owner", ownerLogin), ("$itemId", item.Id));
                }
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                if (item.ItemUid is not null) continue;
                long uid;
                if (item.Type == 0)
                {
                    await ExecuteAsync(tx, "INSERT INTO GoodsObjectList(OwnerLogin,BuyerLogin,ItemID,RegDate,StartDate,EndDate,Period) VALUES($owner,'__PetSystem__',$itemId,$now,$start,$end,$factor);", ct,
                        ("$owner", ownerLogin), ("$itemId", item.Id), ("$now", Format(item.RegDate)), ("$start", Format(item.StartDate)), ("$end", Format(item.EndDate)), ("$factor", item.Factor));
                    uid = (long)(await ScalarTxAsync(tx, "SELECT last_insert_rowid();", ct))!;
                }
                else if (item.Type == 1)
                {
                    await ExecuteAsync(tx, "INSERT INTO DurationItemObjectList(OwnerLogin,BuyerLogin,GoodsID,Duration,RegDate) VALUES($owner,'__PetSystem__',$itemId,$factor,$now);", ct,
                        ("$owner", ownerLogin), ("$itemId", item.Id), ("$factor", item.Factor), ("$now", Format(item.RegDate)));
                    uid = (long)(await ScalarTxAsync(tx, "SELECT last_insert_rowid();", ct))!;
                }
                else continue;
                rows[i] = item with { ItemUid = uid };
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return rows.Select(x => new PetSupplyResult(x.Type, x.Id, x.Factor, Format(x.RegDate), Format(x.StartDate), Format(x.EndDate), x.ItemUid)).ToArray();
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private sealed record WorkItem(int Type, int Id, int Factor, DateTime RegDate, DateTime StartDate, DateTime EndDate, long? ItemUid);
    private async Task<object?> ScalarAsync(string sql, CancellationToken ct, params (string Name, object Value)[] ps)
    { await using var c=_database.Connection.CreateCommand(); c.CommandText=sql; foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value); return await c.ExecuteScalarAsync(ct).ConfigureAwait(false); }
    private static async Task<object?> ScalarTxAsync(SqliteTransaction tx,string sql,CancellationToken ct){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<long?> FindScalarAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){var v=await ScalarTxAsync(tx,sql,ct,ps).ConfigureAwait(false);return v is null?null:Convert.ToInt64(v);}
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
