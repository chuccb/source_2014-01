using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record SkillSlotResult(int Code, int Slot01, int Slot02, int Slot03);
public sealed record SkillSlot2Result(int Code, DateTime EndDate);

public sealed class SkillRepository
{
    private readonly SqliteDatabase _database;

    public SkillRepository(SqliteDatabase database) => _database = database;

    public async Task<int> InsertAsync(
        long unitUid,
        int skillId,
        CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (await ExistsAsync(unitUid, skillId, cancellationToken).ConfigureAwait(false))
            return -1;

        var now = ToSmallDateTime(DateTime.Now);
        await using var tx = (SqliteTransaction)await _database.Connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var rows = await ExecuteAsync(
                tx,
                "INSERT INTO GSkill(UnitUID,SkillID,RegDate) VALUES($unitUid,$skillId,$regDate);",
                cancellationToken,
                ("$unitUid", unitUid),
                ("$skillId", skillId),
                ("$regDate", Format(now)))
                .ConfigureAwait(false);

            if (rows != 1)
                return await RollbackAsync(tx, -11, cancellationToken).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<int> InitializeAsync(
        long unitUid,
        int sPointMax,
        CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await _database.Connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var expected = Convert.ToInt64(await ScalarAsync(
                tx,
                "SELECT COUNT(*) FROM GSkill WHERE UnitUID=$unitUid;",
                cancellationToken,
                ("$unitUid", unitUid)).ConfigureAwait(false));

            var deleted = await ExecuteAsync(
                tx,
                "DELETE FROM GSkill WHERE UnitUID=$unitUid;",
                cancellationToken,
                ("$unitUid", unitUid))
                .ConfigureAwait(false);

            if (deleted != expected)
                return await RollbackAsync(tx, -11, cancellationToken).ConfigureAwait(false);

            var slots = await ExecuteAsync(
                tx,
                "UPDATE GSkillSlot SET Slot01=0,Slot02=0,Slot03=0 WHERE UnitUID=$unitUid;",
                cancellationToken,
                ("$unitUid", unitUid))
                .ConfigureAwait(false);

            if (slots > 1)
                return await RollbackAsync(tx, -12, cancellationToken).ConfigureAwait(false);

            var unit = await ExecuteAsync(
                tx,
                "UPDATE GUnit SET SPoint=$sPointMax WHERE UnitUID=$unitUid;",
                cancellationToken,
                ("$sPointMax", sPointMax),
                ("$unitUid", unitUid))
                .ConfigureAwait(false);

            if (unit != 1)
                return await RollbackAsync(tx, -13, cancellationToken).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<SkillSlotResult> UpsertSlotAsync(
        long unitUid,
        int slot01,
        int slot02,
        int slot03,
        CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await _database.Connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var exists = Convert.ToInt64(await ScalarAsync(
                tx,
                "SELECT EXISTS(SELECT 1 FROM GSkillSlot WHERE UnitUID=$unitUid);",
                cancellationToken,
                ("$unitUid", unitUid)).ConfigureAwait(false)) != 0;

            var sql = exists
                ? "UPDATE GSkillSlot SET Slot01=$slot01,Slot02=$slot02,Slot03=$slot03 WHERE UnitUID=$unitUid;"
                : "INSERT INTO GSkillSlot(UnitUID,Slot01,Slot02,Slot03) VALUES($unitUid,$slot01,$slot02,$slot03);";

            var rows = await ExecuteAsync(
                tx,
                sql,
                cancellationToken,
                ("$unitUid", unitUid),
                ("$slot01", slot01),
                ("$slot02", slot02),
                ("$slot03", slot03))
                .ConfigureAwait(false);

            if (rows != 1)
            {
                var code = exists ? -11 : -12;
                return await RollbackSlotAsync(tx, code, slot01, slot02, slot03, cancellationToken)
                    .ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, slot01, slot02, slot03);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<SkillSlot2Result> UpsertSlot2Async(
        long unitUid,
        int period,
        CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var endDate = ToSmallDateTime(DateTime.Now.AddDays(period));

        await using var tx = (SqliteTransaction)await _database.Connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var exists = Convert.ToInt64(await ScalarAsync(
                tx,
                "SELECT EXISTS(SELECT 1 FROM GSkillSlot2 WHERE UnitUID=$unitUid);",
                cancellationToken,
                ("$unitUid", unitUid)).ConfigureAwait(false)) != 0;

            var sql = exists
                ? "UPDATE GSkillSlot2 SET Slot01=0,Slot02=0,Slot03=0,EndDate=$endDate WHERE UnitUID=$unitUid;"
                : "INSERT INTO GSkillSlot2(UnitUID,Slot01,Slot02,Slot03,EndDate) VALUES($unitUid,0,0,0,$endDate);";

            var rows = await ExecuteAsync(
                tx,
                sql,
                cancellationToken,
                ("$unitUid", unitUid),
                ("$endDate", Format(endDate)))
                .ConfigureAwait(false);

            if (rows != 1)
            {
                var code = exists ? -11 : -12;
                return await RollbackSlot2Async(tx, code, endDate, cancellationToken)
                    .ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, endDate);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<bool> ExistsAsync(long unitUid, int skillId, CancellationToken cancellationToken)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM GSkill WHERE UnitUID=$unitUid AND SkillID=$skillId);";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        command.Parameters.AddWithValue("$skillId", skillId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 0;
    }

    private static async Task<object?> ScalarAsync(
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

    private static async Task<int> RollbackAsync(
        SqliteTransaction tx,
        int value,
        CancellationToken cancellationToken)
    {
        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return value;
    }

    private static async Task<SkillSlotResult> RollbackSlotAsync(
        SqliteTransaction tx,
        int code,
        int slot01,
        int slot02,
        int slot03,
        CancellationToken cancellationToken)
    {
        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return new(code, slot01, slot02, slot03);
    }

    private static async Task<SkillSlot2Result> RollbackSlot2Async(
        SqliteTransaction tx,
        int code,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return new(code, endDate);
    }

    private static DateTime ToSmallDateTime(DateTime value)
    {
        var minute = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
        return value.Second >= 30 ? minute.AddMinutes(1) : minute;
    }

    private static string Format(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");
}
