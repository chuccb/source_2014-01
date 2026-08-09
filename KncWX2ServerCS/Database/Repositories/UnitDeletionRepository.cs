using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class UnitDeletionRepository
{
    private readonly SqliteDatabase _database;

    public UnitDeletionRepository(SqliteDatabase database) => _database = database;

    public async Task<int> DeleteAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        var nickname = await ReadNicknameAsync(unitUid, cancellationToken).ConfigureAwait(false);
        if (nickname is UnitMissing)
            return -1;
        if (nickname.Value is null)
            return -2;

        var now = ToSmallDateTime(DateTime.Now.AddMinutes(1));
        var itemCount = await CountActiveItemsAsync(unitUid, cancellationToken).ConfigureAwait(false);

        await using var transaction = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (await ExecuteAsync(transaction,
                    "UPDATE GUnit SET DelDate = $delDate WHERE UnitUID = $unitUid AND Deleted = 0;",
                    cancellationToken,
                    ("$delDate", FormatSmallDateTime(now)), ("$unitUid", unitUid)) != 1)
                return await RollbackAndReturnAsync(transaction, -11, cancellationToken).ConfigureAwait(false);

            // Native procedure intentionally does not require @@ROWCOUNT = 1 for GTutor.
            await ExecuteAsync(transaction,
                "UPDATE GTutor SET DelDate = $delDate WHERE (TeacherUID = $unitUid OR StudentUID = $unitUid) AND Deleted = 0;",
                cancellationToken,
                ("$delDate", FormatSmallDateTime(now)), ("$unitUid", unitUid));

            if (await ExecuteAsync(transaction,
                    "UPDATE GUnitNickName SET NickName = NULL WHERE UnitUID = $unitUid;",
                    cancellationToken,
                    ("$unitUid", unitUid)) != 1)
                return await RollbackAndReturnAsync(transaction, -12, cancellationToken).ConfigureAwait(false);

            if (await ExecuteAsync(transaction,
                    "INSERT INTO GDeletedNickNameHistory(NickName, UnitUID, Regdate) VALUES ($nickname, $unitUid, $regDate);",
                    cancellationToken,
                    ("$nickname", nickname.Value), ("$unitUid", unitUid), ("$regDate", FormatSmallDateTime(now))) != 1)
                return await RollbackAndReturnAsync(transaction, -14, cancellationToken).ConfigureAwait(false);

            if (await ExecuteAsync(transaction,
                    "UPDATE GItem SET DelDate = $delDate WHERE UnitUID = $unitUid AND Deleted = 0;",
                    cancellationToken,
                    ("$delDate", FormatSmallDateTime(now)), ("$unitUid", unitUid)) != itemCount)
                return await RollbackAndReturnAsync(transaction, -15, cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<(string? Value, bool Missing)> ReadNicknameAsync(long unitUid, CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT NickName FROM GUnitNickName WHERE UnitUID = $unitUid LIMIT 1),
                   EXISTS(SELECT 1 FROM GUnit WHERE UnitUID = $unitUid AND Deleted = 0);
            """;
        command.Parameters.AddWithValue("$unitUid", unitUid);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        await reader.ReadAsync(ct).ConfigureAwait(false);
        var exists = reader.GetInt64(1) != 0;
        if (!exists) return (null, true);
        return (reader.IsDBNull(0) ? null : reader.GetString(0), false);
    }

    private async Task<long> CountActiveItemsAsync(long unitUid, CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM GItem WHERE UnitUID = $unitUid AND Deleted = 0;";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }

    private static async Task<int> ExecuteAsync(SqliteTransaction tx, string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<int> RollbackAndReturnAsync(SqliteTransaction tx, int code, CancellationToken ct)
    {
        await tx.RollbackAsync(ct).ConfigureAwait(false);
        return code;
    }

    private static DateTime ToSmallDateTime(DateTime value)
    {
        var minute = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
        return value.Second >= 30 ? minute.AddMinutes(1) : minute;
    }

    private static string FormatSmallDateTime(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");

    private readonly record struct UnitNickname(string? Value);
    private static readonly UnitNickname UnitMissing = new(null);
}
