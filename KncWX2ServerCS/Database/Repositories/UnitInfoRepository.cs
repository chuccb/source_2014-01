using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class UnitInfoRepository
{
    private readonly SqliteDatabase _database;
    public UnitInfoRepository(SqliteDatabase database) => _database = database;

    public async Task<int> UpdateAsync(long unitUid, int exp, int level, int gamePoint, int vsPoint, int vsPointMax, int sPoint, int win, int lose, int mapId, short spirit, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await ExistsActiveAsync(tx, unitUid, cancellationToken).ConfigureAwait(false)) { await tx.RollbackAsync(cancellationToken).ConfigureAwait(false); return -1; }
            const string unitSql = "UPDATE GUnit SET Exp = Exp + $exp, Level = $level, GamePoint = GamePoint + $gp, VSPoint = VSPoint + $vp, VSPointMax = VSPointMax + $vpm, SPoint = SPoint + $sp, Win = $win, Lose = $lose, LastPosition = $mapId WHERE UnitUID = $unitUid;";
            if (await ExecuteAsync(tx, unitSql, cancellationToken, ("$exp",exp),("$level",level),("$gp",gamePoint),("$vp",vsPoint),("$vpm",vsPointMax),("$sp",sPoint),("$win",win),("$lose",lose),("$mapId",mapId),("$unitUid",unitUid)) != 1) { await tx.RollbackAsync(cancellationToken).ConfigureAwait(false); return -2; }
            if (await ExecuteAsync(tx, "UPDATE GSpirit SET Spirit = $spirit WHERE UnitUID = $unitUid;", cancellationToken, ("$spirit",spirit),("$unitUid",unitUid)) != 1) { await tx.RollbackAsync(cancellationToken).ConfigureAwait(false); return -3; }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false); return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }
    private static async Task<bool> ExistsActiveAsync(SqliteTransaction tx,long id,CancellationToken ct){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText="SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID=$id AND Deleted=0);";c.Parameters.AddWithValue("$id",id);return Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false))!=0;}
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params (string Name,object Value)[] p){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var(n,v)in p)c.Parameters.AddWithValue(n,v);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
}
