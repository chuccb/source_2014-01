using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class QuestRepository
{
    private readonly SqliteDatabase _database;
    public QuestRepository(SqliteDatabase database) => _database = database;

    public async Task<int> CreateAsync(long unitUid, int questId, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var exists = Convert.ToInt64(await ScalarAsync(tx,
                "SELECT EXISTS(SELECT 1 FROM GQuests WHERE UnitUID=$unitUid AND QuestID=$questId);",
                cancellationToken, ("$unitUid", unitUid), ("$questId", questId))) != 0;
            if (exists)
            {
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
                return -1;
            }

            var now = Format(ToSmallDateTime(DateTime.Now));
            var rows = await ExecuteAsync(tx, """
                INSERT INTO GQuests(UnitUID, QuestID, SubQuest0, SubQuest1, SubQuest2, SubQuest3, SubQuest4, RegDate)
                VALUES ($unitUid, $questId, 0, 0, 0, 0, 0, $regDate);
                """, cancellationToken,
                ("$unitUid", unitUid), ("$questId", questId), ("$regDate", now));
            if (rows != 1) return await RollbackAsync(tx, -11, cancellationToken).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<int> UpdateSubQuestAsync(long unitUid, int questId, int index, byte value, CancellationToken cancellationToken = default)
    {
        if (index is < 0 or > 4) return -2;
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = $"UPDATE GQuests SET SubQuest{index}=$value WHERE UnitUID=$unitUid AND QuestID=$questId;";
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$unitUid", unitUid);
        command.Parameters.AddWithValue("$questId", questId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1 ? 0 : -1;
    }

    private static async Task<object?> ScalarAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] ps)
    { await using var c=tx.Connection!.CreateCommand(); c.Transaction=tx; c.CommandText=sql; foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value); return await c.ExecuteScalarAsync(ct).ConfigureAwait(false); }
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params (string Name,object Value)[] ps)
    { await using var c=tx.Connection!.CreateCommand(); c.Transaction=tx; c.CommandText=sql; foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value); return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime value){var m=new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0,value.Kind);return value.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
