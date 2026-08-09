using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record UnitInfo(long UnitUid, int UnitClass, long Exp, int Level, int GamePoint, int VsPoint, int VsPointMax, int BaseHp, int AtkPhysic, int AtkMagic, int DefPhysic, int DefMagic, int SPoint, string NickName, int Win, int Lose, int LastPosition, short Spirit, bool AllSpirit, long OldUid);

public sealed class SpiritRepository
{
    private readonly SqliteDatabase _database;
    public SpiritRepository(SqliteDatabase database) => _database = database;

    public async Task<UnitInfo?> GetUnitInfoAsync(long unitUid, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var c=_database.Connection.CreateCommand();
        c.CommandText="""
SELECT u.UnitUID,u.UnitClass,u.Exp,u.Level,u.GamePoint,u.VSPoint,u.VSPointMax,u.BaseHP,u.AtkPhysic,u.AtkMagic,u.DefPhysic,u.DefMagic,u.SPoint,n.NickName,u.Win,u.Lose,u.LastPosition,
       COALESCE(s.Spirit,0),COALESCE(s.Flag,0),COALESCE(r.OldUID,0)
FROM GUnit u JOIN GUnitNickName n ON n.UnitUID=u.UnitUID
LEFT JOIN GSpirit s ON s.unitUID=u.UnitUID
LEFT JOIN GRecommend r ON r.NewUID=u.UnitUID
WHERE u.UnitUID=$unitUid AND u.Deleted=0;""";
        c.Parameters.AddWithValue("$unitUid",unitUid);
        await using var q=await c.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if(!await q.ReadAsync(ct).ConfigureAwait(false)) return null;
        return new(q.GetInt64(0),q.GetInt32(1),q.GetInt64(2),q.GetInt32(3),q.GetInt32(4),q.GetInt32(5),q.GetInt32(6),q.GetInt32(7),q.GetInt32(8),q.GetInt32(9),q.GetInt32(10),q.GetInt32(11),q.GetInt32(12),q.GetString(13),q.GetInt32(14),q.GetInt32(15),q.GetInt32(16),Convert.ToInt16(q.GetValue(17)),Convert.ToBoolean(q.GetValue(18)),q.GetInt64(19));
    }

    public async Task<int> RefreshOnLoginAsync(long unitUid, DateTime now, CancellationToken ct = default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var unit=await QueryAsync(tx,"SELECT LastDate FROM GUnit WHERE UnitUID=$unitUid AND Deleted=0;",ct,("$unitUid",unitUid));
            if(unit is null)return await RollbackAsync(tx,-1,ct);
            var spirit=await QueryAsync(tx,"SELECT Spirit,RegDate,Flag FROM GSpirit WHERE unitUID=$unitUid;",ct,("$unitUid",unitUid));
            var policy=await QueryRowAsync(tx,"SELECT StartCnt,SupplyCnt,StartSpirit FROM GResurrectionStoneCnt LIMIT 1;",ct);
            if(policy is null)return await RollbackAsync(tx,-98,ct);
            var nowSmall=ToSmallDateTime(now);
            if(spirit is not null && DailyCutoff(Convert.ToDateTime(spirit[1]!),nowSmall) < nowSmall)
            {
                var startSpirit=Convert.ToInt16(policy[2]!);
                if(await ExecAsync(tx,"UPDATE GSpirit SET Spirit=$spirit,Flag=0,RegDate=$regDate WHERE unitUID=$unitUid;",ct,("$spirit",startSpirit),("$regDate",Format(nowSmall)),("$unitUid",unitUid))!=1)return await RollbackAsync(tx,-11,ct);
            }
            var lastLogin=Convert.ToDateTime(unit[0]!);
            if(DailyCutoff(lastLogin,nowSmall)<nowSmall)
            {
                var stone=await QueryAsync(tx,"SELECT Quantity FROM GResurrectionStone WHERE UnitUID=$unitUid;",ct,("$unitUid",unitUid));
                if(stone is null)
                {
                    var start=Convert.ToInt16(policy[0]!);
                    if(await ExecAsync(tx,"INSERT INTO GResurrectionStone(UnitUID,Quantity) VALUES($unitUid,$quantity);",ct,("$unitUid",unitUid),("$quantity",start))!=1)return await RollbackAsync(tx,-21,ct);
                }
                else
                {
                    var quantity=Convert.ToInt32(stone[0]!); var supply=Convert.ToInt16(policy[1]!);
                    if(quantity<supply && await ExecAsync(tx,"UPDATE GResurrectionStone SET Quantity=$supply WHERE UnitUID=$unitUid;",ct,("$supply",supply),("$unitUid",unitUid))!=1)return await RollbackAsync(tx,-22,ct);
                }
            }
            if(lastLogin.Date<nowSmall.Date && await ExecAsync(tx,"UPDATE GUnit SET PlayDayCnt=PlayDayCnt+1 WHERE UnitUID=$unitUid;",ct,("$unitUid",unitUid))!=1)return await RollbackAsync(tx,-31,ct);
            if(await ExecAsync(tx,"UPDATE GUnit SET LoginCount=LoginCount+1,LastDate=$now WHERE UnitUID=$unitUid;",ct,("$now",Format(nowSmall)),("$unitUid",unitUid))!=1)return await RollbackAsync(tx,-32,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false); return 0;
        }
        catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    private static DateTime DailyCutoff(DateTime source,DateTime now){var d=source.AddHours(18).Date.AddHours(6);return d;}
    private async Task<object[]?> QueryAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);await using var r=await c.ExecuteReaderAsync(ct).ConfigureAwait(false);if(!await r.ReadAsync(ct).ConfigureAwait(false))return null;var a=new object[r.FieldCount];r.GetValues(a);return a;}
    private async Task<object[]?> QueryRowAsync(SqliteTransaction tx,string sql,CancellationToken ct){return await QueryAsync(tx,sql,ct);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<int> RollbackAsync(SqliteTransaction tx,int value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime v){var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind);return v.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
