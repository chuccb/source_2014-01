using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record CreateUnitResult(int Code, long UnitUid, DateTime NicknameAvailableAt);

public sealed class UnitCreationRepository
{
    private readonly SqliteDatabase _database;

    public UnitCreationRepository(SqliteDatabase database) => _database = database;

    public async Task<CreateUnitResult> CreateAsync(
        long userUid,
        string nickname,
        byte unitClass,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        if (nickname.Length > 16)
            throw new ArgumentOutOfRangeException(nameof(nickname));

        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        var now = DateTime.Now;
        var startSpirit = await GetStartSpiritAsync(cancellationToken).ConfigureAwait(false);
        var user = await GetUserAsync(userUid, cancellationToken).ConfigureAwait(false);

        if (user is null || user.Value.Deleted)
            return new(-1, 0, new DateTime(2000, 1, 1));

        var activeUnitCount = await ScalarLongAsync(
            "SELECT COUNT(*) FROM GUnit WHERE Deleted = 0 AND UserUID = $userUid;",
            cancellationToken,
            ("$userUid", userUid));
        if (activeUnitCount >= user.Value.UserSize)
            return new(-3, 0, new DateTime(2000, 1, 1));

        var nicknameExists = await ScalarLongAsync(
            "SELECT EXISTS(SELECT 1 FROM GUnitNickName WHERE NickName = $nickname);",
            cancellationToken,
            ("$nickname", nickname));
        if (nicknameExists != 0)
            return new(-2, 0, new DateTime(2000, 1, 1));

        var deletedNicknameDate = await GetDeletedNicknameDateAsync(nickname, cancellationToken).ConfigureAwait(false);
        if (deletedNicknameDate.HasValue && now.AddDays(-14) < deletedNicknameDate.Value)
            return new(-222, 0, deletedNicknameDate.Value.AddDays(14));

        await using var transaction = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var unitUid = await InsertUnitAsync(transaction, userUid, unitClass, now, cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(transaction,
                "INSERT INTO GUnitNickName(UnitUID, NickName, RegDate) VALUES ($unitUid, $nickname, $now);",
                cancellationToken,
                ("$unitUid", unitUid), ("$nickname", nickname), ("$now", now.ToString("yyyy-MM-dd HH:mm:ss"))).ConfigureAwait(false);

            for (var question = 1; question <= 4; question++)
            {
                await ExecuteAsync(transaction,
                    "INSERT INTO GDenyOption(UnitUID, QuestionNo, CodeNo) VALUES ($unitUid, $question, 1);",
                    cancellationToken,
                    ("$unitUid", unitUid), ("$question", question)).ConfigureAwait(false);
            }

            await ExecuteAsync(transaction,
                "INSERT INTO GQuests(UnitUID, QuestID, SubQuest0, SubQuest1, SubQuest2, SubQuest3, SubQuest4, RegDate) VALUES ($unitUid, 13, 1, 0, 0, 0, 0, $now);",
                cancellationToken,
                ("$unitUid", unitUid), ("$now", now.ToString("yyyy-MM-dd HH:mm:ss"))).ConfigureAwait(false);

            var skillId = unitClass switch
            {
                1 => 10000,
                2 => 20030,
                3 => 30000,
                4 => 40010,
                _ => (int?)null
            };
            if (skillId.HasValue)
            {
                await ExecuteAsync(transaction,
                    "INSERT INTO GSkill(UnitUID, SkillID, RegDate) VALUES ($unitUid, $skillId, $now);",
                    cancellationToken,
                    ("$unitUid", unitUid), ("$skillId", skillId.Value), ("$now", now.ToString("yyyy-MM-dd HH:mm:ss"))).ConfigureAwait(false);
                await ExecuteAsync(transaction,
                    "INSERT INTO GSkillSlot(UnitUID, Slot01, Slot02, Slot03) VALUES ($unitUid, $skillId, 0, 0);",
                    cancellationToken,
                    ("$unitUid", unitUid), ("$skillId", skillId.Value)).ConfigureAwait(false);
            }

            await ExecuteAsync(transaction,
                "INSERT INTO GSpirit(unitUID, Spirit, RegDate, Flag) VALUES ($unitUid, $spirit, $now, 0);",
                cancellationToken,
                ("$unitUid", unitUid), ("$spirit", startSpirit), ("$now", now.ToString("yyyy-MM-dd HH:mm:ss"))).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, unitUid, new DateTime(2000, 1, 1));
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<(bool Deleted, long UserSize)?> GetUserAsync(long userUid, CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT Deleted, USSize FROM GUser WHERE UserUID = $userUid LIMIT 1;";
        command.Parameters.AddWithValue("$userUid", userUid);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        return (reader.GetInt64(0) != 0, reader.GetInt64(1));
    }

    private async Task<short> GetStartSpiritAsync(CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT StartSpirit FROM GResurrectionStoneCnt LIMIT 1;";
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return value is null || value is DBNull ? (short)0 : Convert.ToInt16(value);
    }

    private async Task<DateTime?> GetDeletedNicknameDateAsync(string nickname, CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT RegDate FROM GDeletedNickNameHistory WHERE NickName = $nickname ORDER BY RegDate DESC LIMIT 1;";
        command.Parameters.AddWithValue("$nickname", nickname);
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return value is null || value is DBNull ? null : DateTime.Parse(Convert.ToString(value)!);
    }

    private async Task<long> InsertUnitAsync(SqliteTransaction transaction, long userUid, byte unitClass, DateTime now, CancellationToken ct)
    {
        await using var command = _database.Connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO GUnit(UserUID, UnitClass, Exp, Level, GamePoint, VSPoint, VSPointMax, BaseHP,
                AtkPhysic, AtkMagic, DefPhysic, DefMagic, SPoint, Win, Lose, Seceder,
                RegDate, DelDate, LastPosition)
            VALUES ($userUid, $unitClass, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, $now, $now, 20000);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$userUid", userUid);
        command.Parameters.AddWithValue("$unitClass", unitClass);
        command.Parameters.AddWithValue("$now", now.ToString("yyyy-MM-dd HH:mm:ss"));
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }

    private async Task<long> ScalarLongAsync(string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }

    private static async Task ExecuteAsync(SqliteTransaction transaction, string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}
