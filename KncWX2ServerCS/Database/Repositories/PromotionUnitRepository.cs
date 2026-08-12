using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record PromotionUnitResult(int Code, long[] UnitUids);

public sealed class PromotionUnitRepository
{
    private static readonly (int UnitClass, int[] ItemIds)[] PromotionItems =
    [
        (1, [128000, 128001, 128002, 128003, 128004]),
        (2, [128010, 128011, 128012, 128013, 128014]),
        (3, [128005, 128006, 128007, 128008, 128009]),
        (4, [128072, 128073, 128074, 128075, 128076]),
        (5, [130134, 130135, 130136, 130137, 130138]),
    ];

    private readonly SqliteDatabase _database;

    public PromotionUnitRepository(SqliteDatabase database) => _database = database;

    public async Task<PromotionUnitResult> CreateAsync(
        long userUid,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!await UserExistsAsync(userUid, cancellationToken).ConfigureAwait(false))
            return new(-1, []);

        var nicknames = Enumerable.Range(1, PromotionItems.Length)
            .Select(index => $"{userId}{index}")
            .ToArray();

        await using var transaction = (SqliteTransaction)
            await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (await NicknamesExistAsync(transaction, nicknames, cancellationToken).ConfigureAwait(false))
                return await RollbackAsync(transaction, new(-2, []), cancellationToken).ConfigureAwait(false);

            var unitUids = new List<long>(PromotionItems.Length);

            foreach (var (unitClass, itemIds) in PromotionItems)
            {
                var unitUid = await InsertUnitAsync(
                    transaction,
                    userUid,
                    nicknames[unitClass - 1],
                    unitClass,
                    cancellationToken).ConfigureAwait(false);

                if (unitUid <= 0)
                    return await RollbackAsync(transaction, new(-10, []), cancellationToken).ConfigureAwait(false);

                unitUids.Add(unitUid);

                foreach (var itemId in itemIds)
                    await InsertItemAsync(transaction, unitUid, itemId, cancellationToken).ConfigureAwait(false);

                await InsertDungeonClearAsync(transaction, unitUid, cancellationToken).ConfigureAwait(false);
                await UpdatePromotionStatsAsync(transaction, unitUid, cancellationToken).ConfigureAwait(false);
                await EquipPromotionItemsAsync(transaction, unitUid, itemIds, cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, unitUids.ToArray());
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<bool> UserExistsAsync(long userUid, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM GUser WHERE UserUID = $userUid AND Deleted = 0);";
        var value = await ScalarAsync(sql, cancellationToken, ("$userUid", userUid)).ConfigureAwait(false);
        return Convert.ToInt64(value) != 0;
    }

    private static async Task<bool> NicknamesExistAsync(
        SqliteTransaction transaction,
        string[] nicknames,
        CancellationToken cancellationToken)
    {
        var placeholders = string.Join(", ", nicknames.Select((_, index) => $"$nickname{index}"));

        await using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM GUnitNickName WHERE NickName IN ({placeholders});";

        for (var index = 0; index < nicknames.Length; index++)
            command.Parameters.AddWithValue($"$nickname{index}", nicknames[index]);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        return count > 0;
    }

    private static async Task<long> InsertUnitAsync(
        SqliteTransaction transaction,
        long userUid,
        string nickname,
        int unitClass,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        await using var unitCommand = transaction.Connection!.CreateCommand();
        unitCommand.Transaction = transaction;
        unitCommand.CommandText = """
            INSERT INTO GUnit
                (UserUID, UnitClass, Exp, Level, GamePoint, VSPoint, VSPointMax,
                 BaseHP, AtkPhysic, AtkMagic, DefPhysic, DefMagic, SPoint,
                 Win, Lose, Seceder, RegDate, DelDate, LastPosition)
            VALUES
                ($userUid, $unitClass, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1,
                 0, 0, 0, $now, $now, 20000);
            SELECT last_insert_rowid();
            """;
        unitCommand.Parameters.AddWithValue("$userUid", userUid);
        unitCommand.Parameters.AddWithValue("$unitClass", unitClass);
        unitCommand.Parameters.AddWithValue("$now", now);

        var unitUid = Convert.ToInt64(
            await unitCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));

        await using var nicknameCommand = transaction.Connection!.CreateCommand();
        nicknameCommand.Transaction = transaction;
        nicknameCommand.CommandText = """
            INSERT INTO GUnitNickName(UnitUID, NickName, RegDate)
            VALUES ($unitUid, $nickname, $now);
            """;
        nicknameCommand.Parameters.AddWithValue("$unitUid", unitUid);
        nicknameCommand.Parameters.AddWithValue("$nickname", nickname);
        nicknameCommand.Parameters.AddWithValue("$now", now);
        await nicknameCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await ExecuteAsync(
            transaction,
            "INSERT INTO GSpirit(UnitUID, Spirit, RegDate, Flag) " +
            "SELECT $unitUid, StartSpirit, $now, 0 FROM GResurrectionStoneCnt LIMIT 1;",
            cancellationToken,
            ("$unitUid", unitUid),
            ("$now", now)).ConfigureAwait(false);

        return unitUid;
    }

    private static Task<int> InsertItemAsync(
        SqliteTransaction transaction,
        long unitUid,
        int itemId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            transaction,
            "INSERT INTO GItem(UnitUID, ItemID, InventoryCategory, SlotID, RegDate, DelDate) " +
            "VALUES($unitUid, $itemId, 0, 0, $now, $now);",
            cancellationToken,
            ("$unitUid", unitUid),
            ("$itemId", itemId),
            ("$now", DateTime.Now.ToString("yyyy-MM-dd HH:mm")));

    private static async Task InsertDungeonClearAsync(
        SqliteTransaction transaction,
        long unitUid,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH Modes(GameMode) AS
            (
                VALUES
                    (30000), (30001), (30002),
                    (30010), (30011), (30012),
                    (30020), (30021), (30022),
                    (30030), (30031), (30032),
                    (30040), (30041), (30042),
                    (30050), (30051), (30052),
                    (30060), (30061), (30062),
                    (30070), (30071), (30072),
                    (30080), (30081), (30082),
                    (30090), (30091), (30092),
                    (30100), (30101), (30102),
                    (30110), (30111), (30112),
                    (30120), (30121), (30122),
                    (30130), (30131), (30132),
                    (30140), (30141), (30142),
                    (30150), (30151), (30152),
                    (30160), (30161), (30162),
                    (30170), (30171), (30172),
                    (30180), (30181), (30182),
                    (30190), (30191), (30192),
                    (30200), (30201), (30202),
                    (30210), (30211), (30212),
                    (30220), (30221), (30222),
                    (30230), (30231), (30232),
                    (30240), (30241), (30242),
                    (30250), (30251), (30252),
                    (30260), (30261), (30262),
                    (30270), (30271), (30272),
                    (30280), (30281), (30282),
                    (30290), (30291), (30292),
                    (30300), (30301), (30302)
            )
            INSERT INTO GDungeonClear
                (UnitUID, GameMode, MaxScore, MaxTotalRank, RegDate)
            SELECT $unitUid, GameMode, 0, 0, $now
            FROM Modes;
            """;

        await ExecuteAsync(
            transaction,
            sql,
            cancellationToken,
            ("$unitUid", unitUid),
            ("$now", DateTime.Now.ToString("yyyy-MM-dd HH:mm"))).ConfigureAwait(false);
    }

    private static Task<int> UpdatePromotionStatsAsync(
        SqliteTransaction transaction,
        long unitUid,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            transaction,
            "UPDATE GUnit SET Exp = 6500000, GamePoint = 50000000, SPoint = 801 " +
            "WHERE UnitUID = $unitUid;",
            cancellationToken,
            ("$unitUid", unitUid));

    private static async Task EquipPromotionItemsAsync(
        SqliteTransaction transaction,
        long unitUid,
        int[] itemIds,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < itemIds.Length; index++)
        {
            var slotId = index == 0 ? 10 : (index + 1) * 2;

            await ExecuteAsync(
                transaction,
                "UPDATE GItem SET InventoryCategory = 9, SlotID = $slot " +
                "WHERE UnitUID = $unitUid AND ItemID = $itemId;",
                cancellationToken,
                ("$slot", slotId),
                ("$unitUid", unitUid),
                ("$itemId", itemIds[index])).ConfigureAwait(false);
        }
    }

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

    private static async Task<T> RollbackAsync<T>(
        SqliteTransaction transaction,
        T result,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static async Task<int> ExecuteAsync(
        SqliteTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
