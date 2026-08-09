using System.Globalization;
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
/// SQLite implementation of dbo.gup_select_unit.
/// </summary>
public sealed class UnitSelectionRepository
{
    private readonly SqliteDatabase _database;

    public UnitSelectionRepository(SqliteDatabase database) => _database = database;

    public async Task<SelectUnitResult> SelectAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        var unit = await ReadUnitAsync(unitUid, cancellationToken).ConfigureAwait(false);
        if (unit is null)
            return new(-1, 0, false, 0, 0, 0, default);

        await using var transaction = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = ToSmallDateTime(DateTime.Now);
            var spirit = await ReadSpiritAsync(transaction, unitUid, cancellationToken).ConfigureAwait(false);
            var policy = await ReadResurrectionPolicyAsync(transaction, cancellationToken).ConfigureAwait(false);

            // Native gup_select_unit performs an UPDATE and requires exactly one row
            // when the daily Spirit reset is due. A missing GSpirit row therefore maps
            // to the same failure code rather than silently succeeding.
            if (spirit is null)
            {
                if (IsAfterDailyReset(DateTime.MinValue, now))
                    return await FailAsync(transaction, -11, cancellationToken).ConfigureAwait(false);
            }
            else if (IsAfterDailyReset(spirit.Value.RegDate, now))
            {
                await ExecuteAsync(transaction,
                    "UPDATE GSpirit SET Spirit = $spirit, Flag = 0, RegDate = $regDate WHERE UnitUID = $unitUid;",
                    cancellationToken,
                    ("$spirit", policy.StartSpirit),
                    ("$regDate", FormatSmallDateTime(now)),
                    ("$unitUid", unitUid));

                spirit = (policy.StartSpirit, false, now);
            }

            var stone = await ReadStoneAsync(transaction, unitUid, cancellationToken).ConfigureAwait(false);
            if (IsAfterDailyReset(unit.Value.LastDate, now))
            {
                if (stone is null)
                {
                    await ExecuteAsync(transaction,
                        "INSERT INTO GResurrectionStone(UnitUID, Quantity) VALUES ($unitUid, $quantity);",
                        cancellationToken,
                        ("$unitUid", unitUid),
                        ("$quantity", policy.StartCount));
                    stone = policy.StartCount;
                }
                else if (stone.Value < policy.SupplyCount)
                {
                    var refillAmount = policy.SupplyCount - stone.Value;
                    await ExecuteAsync(transaction,
                        "UPDATE GResurrectionStone SET Quantity = $quantity WHERE UnitUID = $unitUid;",
                        cancellationToken,
                        ("$quantity", policy.SupplyCount),
                        ("$unitUid", unitUid));

                    // Statistics.dbo.StatsStoneCnt is not part of the verified SQLite
                    // schema. Its migration remains intentionally isolated instead of
                    // inventing a table definition. The exact refill amount is retained
                    // here for the future Statistics repository.
                    _ = refillAmount;
                }
            }

            var playDayCount = unit.Value.PlayDayCount;
            if (unit.Value.LastDate.Date != now.Date)
            {
                await ExecuteAsync(transaction,
                    "UPDATE GUnit SET PlayDayCnt = PlayDayCnt + 1 WHERE UnitUID = $unitUid;",
                    cancellationToken,
                    ("$unitUid", unitUid));
                playDayCount++;
            }

            var loginCount = unit.Value.LoginCount + 1;
            await ExecuteAsync(transaction,
                "UPDATE GUnit SET LoginCount = LoginCount + 1, LastDate = $lastDate WHERE UnitUID = $unitUid;",
                cancellationToken,
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

    private static async Task<SelectUnitResult> FailAsync(SqliteTransaction transaction, int code, CancellationToken ct)
    {
        await transaction.RollbackAsync(ct).ConfigureAwait(false);
        return new(code, 0, false, 0, 0, 0, default);
    }

    private async Task<UnitState?> ReadUnitAsync(long unitUid, CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = """
            SELECT LastDate, PlayDayCnt, LoginCount
            FROM GUnit
            WHERE UnitUID = $unitUid AND Deleted = 0
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$unitUid", unitUid);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        return new UnitState(
            ParseSmallDateTime(reader.GetString(0)),
            Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
            Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture));
    }

    private static async Task<(short Spirit, bool Flag, DateTime RegDate)?> ReadSpiritAsync(SqliteTransaction tx, long unitUid, CancellationToken ct)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "SELECT Spirit, Flag, RegDate FROM GSpirit WHERE UnitUID = $unitUid LIMIT 1;";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        return (
            Convert.ToInt16(reader.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture) != 0,
            ParseSmallDateTime(reader.GetString(2)));
    }

    private static async Task<Policy> ReadResurrectionPolicyAsync(SqliteTransaction tx, CancellationToken ct)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "SELECT StartCnt, SupplyCnt, StartSpirit FROM GResurrectionStoneCnt LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return new(0, 0, 0);
        return new(
            ReadNullableInt16(reader.GetValue(0)),
            ReadNullableInt16(reader.GetValue(1)),
            ReadNullableInt16(reader.GetValue(2)));
    }

    private static async Task<short?> ReadStoneAsync(SqliteTransaction tx, long unitUid, CancellationToken ct)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "SELECT Quantity FROM GResurrectionStone WHERE UnitUID = $unitUid LIMIT 1;";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return value is null || value is DBNull
            ? null
            : Convert.ToInt16(value, CultureInfo.InvariantCulture);
    }

    private static short ReadNullableInt16(object value) =>
        value is null || value is DBNull
            ? (short)0
            : Convert.ToInt16(value, CultureInfo.InvariantCulture);

    private static async Task ExecuteAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static bool IsAfterDailyReset(DateTime source, DateTime now)
    {
        var shifted = source.AddHours(18);
        var reset = new DateTime(shifted.Year, shifted.Month, shifted.Day, 6, 0, 0, shifted.Kind);
        return reset < now;
    }

    private static DateTime ToSmallDateTime(DateTime value)
    {
        var baseMinute = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
        return value.Second >= 30 ? baseMinute.AddMinutes(1) : baseMinute;
    }

    private static DateTime ParseSmallDateTime(string value) =>
        DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None);

    private static string FormatSmallDateTime(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private readonly record struct UnitState(DateTime LastDate, int PlayDayCount, int LoginCount);
    private readonly record struct Policy(short StartCount, short SupplyCount, short StartSpirit);
}
