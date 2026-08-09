using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class UnitRepository
{
    private readonly SqliteDatabase _database;
    public UnitRepository(SqliteDatabase database) => _database = database;

    public async Task<int> UpdateInfoAsync(long unitUid,int exp,int level,int gamePoint,int vsPoint,int vsPointMax,int sPoint,int win,int lose,int mapId,short spirit,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if(!await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID=$uid AND Deleted=0);",ct,("$uid",unitUid)))return -1;
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try{
            if(await ExecAsync(tx,"UPDATE GUnit SET Exp=Exp+$exp,Level=$level,GamePoint=GamePoint+$gp,VSPoint=VSPoint+$vp,VSPointMax=VSPointMax+$vpm,SPoint=SPoint+$sp,Win=$win,Lose=$lose,LastPosition=$map WHERE UnitUID=$uid;",ct,("$exp",exp),("$level",level),("$gp",gamePoint),("$vp",vsPoint),("$vpm",vsPointMax),("$sp",sPoint),("$win",win),("$lose",lose),("$map",mapId),("$uid",unitUid))!=1)return await RollbackAsync(tx,-2,ct);
            if(await ExecAsync(tx,"UPDATE GSpirit SET Spirit=$spirit WHERE unitUID=$uid;",ct,("$spirit",spirit),("$uid",unitUid))!=1)return await RollbackAsync(tx,-3,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    public async Task<int> UpdateClassAsync(long unitUid,byte unitClass,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);if(!await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID=$uid AND Deleted=0);",ct,("$uid",unitUid)))return -1;
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);try{if(await ExecAsync(tx,"UPDATE GUnit SET UnitClass=$class WHERE UnitUID=$uid;",ct,("$class",unitClass),("$uid",unitUid))!=1)return await RollbackAsync(tx,-2,ct);await tx.CommitAsync(ct).ConfigureAwait(false);return 0;}catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    public async Task<int> UpdateLoginAsync(long unitUid,DateTime now,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);var last=await ScalarAsync("SELECT LastDate FROM GUnit WHERE UnitUID=$uid AND Deleted=0;",ct,("$uid",unitUid));if(last is null)return -1;
        var current=ToSmallDateTime(now);await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);try{
            if(Convert.ToDateTime(last).Date<current.Date&&await ExecAsync(tx,"UPDATE GUnit SET PlayDayCnt=PlayDayCnt+1 WHERE UnitUID=$uid;",ct,("$uid",unitUid))!=1)return await RollbackAsync(tx,-2,ct);
            if(await ExecAsync(tx,"UPDATE GUnit SET LoginCount=LoginCount+1,LastDate=$now WHERE UnitUID=$uid;",ct,("$now",Format(current)),("$uid",unitUid))!=1)return await RollbackAsync(tx,-2,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    private async Task<bool> ExistsAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps)=>Convert.ToInt64(await ScalarAsync(sql,ct,ps).ConfigureAwait(false))!=0;
    private async Task<object?> ScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<int> RollbackAsync(SqliteTransaction tx,int value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime v){var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind);return v.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
