using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record SelectUnitResult(
    int Code,
    short Spirit,
    bool SpiritFlag,
    short ResurrectionStoneQuantity,
    int PlayDayCount,
    int LoginCount,
    DateTime LastLogin);

/// <summary>
/// SQLite implementation of dbo.gup_select_unit. The procedure's business
/// semantics are preserved; SQL Server locking hints are represented by the
/// single SQLite transaction and its serialized write section.
/// </summary>
public sealed class UnitSelectionRepository
{
    private readonly SqliteDatabase _database;

    public UnitSelectionRepository(SqliteDatabase database) => _database = database;

    public async Task<SelectUnitResult> SelectAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        var unit = await ReadUnitAsync(unitUid, cancellationToken).ConfigureAwait(false);
        if (unit is null || unit.Value.Deleted)
            return new(-1, 0, false, 0, 0, 0, default);

        await using var transaction = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = ToSmallDateTime(DateTime.Now);
            var spirit = await ReadSpiritAsync(transaction, unitUid, cancellationToken).ConfigureAwait(false);
            var policy = await ReadResurrectionPolicyAsync(transaction, cancellationToken).ConfigureAwait(false);

            if (spirit is not null && IsAfterDailyReset(spirit.Value.RegDate, now))
            {
                spirit = spirit.Value with { Spirit = policy.StartSpirit, Flag = false, RegDate = now };
                await ExecuteAsync(transaction,
                    "UPDATE GSpirit SET Spirit = $spirit, Flag = 0, RegDate = $regDate WHERE UnitUID = $unitUid;",
                    cancellationToken,
                    ("$spirit", policy.StartSpirit), ("$regDate", FormatSmallDateTime(now)), ("$unitUid", unitUid));
            }

            var stone = await ReadStoneAsync(transaction, unitUid, cancellationToken).ConfigureAwait(false);
            if (IsAfterDailyReset(unit.LastDate, now))
            {
                if (stone is null)
                {
                    await ExecuteAsync(transaction,
                        "INSERT INTO GResurrectionStone(UnitUID, Quantity) VALUES ($unitUid, $quantity);",
                        cancellationToken,
                        ("$unitUid", unitUid), ("$quantity", policy.StartCount));
                    stone = policy.StartCount;
                }
                else if (stone.Value < policy.SupplyCount)
                {
                    var supplied = policy.SupplyCount - stone.Value;
                    await ExecuteAsync(transaction,
                        "UPDATE GResurrectionStone SET Quantity = $quantity WHERE UnitUID = $unitUid;",
                        cancellationToken,
                        ("$quantity", policy.SupplyCount), ("$unitUid", unitUid));

                    // The legacy procedure also updates Statistics.dbo.StatsStoneCnt.
                    // That database is outside the game DB and has no verified SQLite
                    // schema yet, so this migration intentionally does not invent it.
                }
            }

            var playDayCount = unit.PlayDayCount;
            if (unit.LastDate.Date != now.Date)
                playDayCount++;

            var loginCount = unit.LoginCount + 1;
            await ExecuteAsync(transaction,
                "UPDATE GUnit SET PlayDayCnt = $playDayCnt, LoginCount = $loginCount, LastDate = $lastDate WHERE UnitUID = $unitUid;",
                cancellationToken,
                ("$playDayCnt", playDayCount),
                ("$loginCount", loginCount),
                ("$lastDate", FormatSmallDateTime(now)),
                ("$unitUid", unitUid));

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new(
                0,
                spirit?.Spirit ?? 0,
                spirit?.Flag ?? false,
                stone ?? 0,
                playDayCount,
                loginCount,
                now);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<UnitState?> ReadUnitAsync(long unitUid, CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = """
            SELECT Deleted, LastDate, PlayDayCnt, LoginCount
            FROM GUnit
            WHERE UnitUID = $unitUid AND Deleted = 0
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$unitUid", unitUid);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        return new UnitState(
            reader.GetInt64(0) != 0,
            ParseSmallDateTime(reader.GetString(1)),
            reader.GetInt32(2),
            reader.GetInt32(3));
    }

    private static async Task<(short Spirit, bool Flag, DateTime RegDate)?> ReadSpiritAsync(SqliteTransaction tx, long unitUid, CancellationToken ct)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "SELECT Spirit, Flag, RegDate FROM GSpirit WHERE UnitUID = $unitUid LIMIT 1;";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        return (Convert.ToInt16(reader.GetValue(0)), Convert.ToInt64(reader.GetValue(1)) != 0, ParseSmallDateTime(reader.GetString(2)));
    }

    private static async Task<Policy> ReadResurrectionPolicyAsync(SqliteTransaction tx, CancellationToken ct)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "SELECT StartCnt, SupplyCnt, StartSpirit FROM GResurrectionStoneCnt LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return new(0, 0, 0);
        return new(Convert.ToInt16(reader.GetValue(0)), Convert.ToInt16(reader.GetValue(1)), Convert.ToInt16(reader.GetValue(2)));
    }

    private static async Task<short?> ReadStoneAsync(SqliteTransaction tx, long unitUid, CancellationToken ct)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "SELECT Quantity FROM GResurrectionStone WHERE UnitUID = $unitUid LIMIT 1;";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return value is null || value is DBNull ? null : Convert.ToInt16(value);
    }

    private static async Task ExecuteAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static bool IsAfterDailyReset(DateTime source, DateTime now)
    {
        // Native expression: CONVERT(smalldatetime,
        // CONVERT(nvarchar(10), DATEADD(hh,+18, source),120)+' 06:00') < now.
        // This is equivalent to the next 06:00 reset after shifting the source
        // timestamp by +18 hours, expressed without locale-dependent parsing.
        var shifted = source.AddHours(18);
        var reset = new DateTime(shifted.Year, shifted.Month, shifted.Day, 6, 0, 0, shifted.Kind);
        return reset < now;
    }

    private static DateTime ToSmallDateTime(DateTime value)
    {
        var baseMinute = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
        return value.Second >= 30 ? baseMinute.AddMinutes(1) : baseMinute;
    }

    private static DateTime ParseSmallDateTime(string value) => DateTime.Parse(value);
    private static string FormatSmallDateTime(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");

    private readonly record struct UnitState(bool Deleted, DateTime LastDate, int PlayDayCount, int LoginCount);
    private readonly record struct Policy(short StartCount, short SupplyCount, short StartSpirit);
}
