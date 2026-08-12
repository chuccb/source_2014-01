using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class DungeonRepository
{
    private readonly SqliteDatabase _database;

    public DungeonRepository(SqliteDatabase database) => _database = database;

    // Matches gup_all_dungeonclear: insert the complete fixed GameMode set and return no error code.
    public async Task<int> AllDungeonClearAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = _database.Connection.CreateCommand();
        command.CommandText = """
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
            SELECT $unitUid, GameMode, 0, 0, $regDate
            FROM Modes;
            """;
        command.Parameters.AddWithValue("$unitUid", unitUid);
        command.Parameters.AddWithValue("$regDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
