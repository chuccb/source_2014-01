using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record SkillSlotResult(int Code, int Slot01, int Slot02, int Slot03);
public sealed record SkillSlot2Result(int Code, DateTime EndDate);

public sealed class SkillRepository
{
    private readonly SqliteDatabase _database;
    public SkillRepository(SqliteDatabase database) => _database = database;

    public async Task<int> InsertAsync(long unitUid, int skillId, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (await ExistsAsync(unitUid, skillId, cancellationToken).ConfigureAwait(false)) return -1;
        var now = ToSmallDateTime(DateTime.Now);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var rows = await ExecuteAsync(tx, "INSERT INTO GSkill(UnitUID, SkillID, RegDate) VALUES ($unitUid, $skillId, $regDate);", cancellationToken,
                ("$unitUid", unitUid), ("$skillId", skillId), ("$regDate", Format(now)));
            if (rows != 1) return await RollbackAsync(tx, -11, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    public async Task<SkillSlotResult> UpsertSlotAsync(long unitUid, int slot01, int slot02, int slot03, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var exists = Convert.ToInt64(await ScalarAsync(tx, "SELECT EXISTS(SELECT 1 FROM GSkillSlot WHERE UnitUID = $unitUid);", cancellationToken, ("$unitUid", unitUid))) != 0;
            var sql = exists
                ? "UPDATE GSkillSlot SET Slot01=$slot01, Slot02=$slot02, Slot03=$slot03 WHERE UnitUID=$unitUid;"
                : "INSERT INTO GSkillSlot(UnitUID, Slot01, Slot02, Slot03) VALUES($unitUid,$slot01,$slot02,$slot03);";
            var rows = await ExecuteAsync(tx, sql, cancellationToken, ("$unitUid", unitUid), ("$slot01", slot01), ("$slot02", slot02), ("$slot03", slot03));
            if (rows != 1) return await RollbackAsync(tx, exists ? -11 : -12, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, slot01, slot02, slot03);
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    public async Task<SkillSlot2Result> UpsertSlot2Async(long unitUid, int period, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var endDate = ToSmallDateTime(DateTime.Now.AddDays(period));
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var exists = Convert.ToInt64(await ScalarAsync(tx, "SELECT EXISTS(SELECT 1 FROM GSkillSlot2 WHERE UnitUID = $unitUid);", cancellationToken, ("$unitUid", unitUid))) != 0;
            var sql = exists
                ? "UPDATE GSkillSlot2 SET Slot01=0, Slot02=0, Slot03=0, EndDate=$endDate WHERE UnitUID=$unitUid;"
                : "INSERT INTO GSkillSlot2(UnitUID, Slot01, Slot02, Slot03, EndDate) VALUES($unitUid,0,0,0,$endDate);";
            var rows = await ExecuteAsync(tx, sql, cancellationToken, ("$unitUid", unitUid), ("$endDate", Format(endDate)));
            if (rows != 1) return await RollbackAsync(tx, exists ? -11 : -12, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, endDate);
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    private async Task<bool> ExistsAsync(long unitUid, int skillId, CancellationToken ct)
    {
        await using var cmd = _database.Connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM GSkill WHERE UnitUID=$unitUid AND SkillID=$skillId);";
        cmd.Parameters.AddWithValue("$unitUid", unitUid); cmd.Parameters.AddWithValue("$skillId", skillId);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 0;
    }
    private static async Task<object?> ScalarAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] ps)
    { await using var c=tx.Connection!.CreateCommand(); c.Transaction=tx; c.CommandText=sql; foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value); return await c.ExecuteScalarAsync(ct).ConfigureAwait(false); }
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params (string Name,object Value)[] ps)
    { await using var c=tx.Connection!.CreateCommand(); c.Transaction=tx; c.CommandText=sql; foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value); return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime value){var m=new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0,value.Kind);return value.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
