using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class NicknameRestoreRepository
{
    private readonly SqliteDatabase _database;
    public NicknameRestoreRepository(SqliteDatabase database) => _database = database;

    public async Task<int> RestoreAsync(long userUid,long unitUid,string newNickname,CancellationToken ct=default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newNickname);
        if(newNickname.Length>16)throw new ArgumentOutOfRangeException(nameof(newNickname));
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if(!await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GUnit WHERE UserUID=$user AND UnitUID=$unit AND Deleted=0);",ct,("$user",userUid),("$unit",unitUid)))return -1;
        var old=await ScalarAsync("SELECT NickName FROM GUnitNickName WHERE UnitUID=$unit;",ct,("$unit",unitUid));
        if(old is null||!string.Equals(Convert.ToString(old),"__DELETED__",StringComparison.Ordinal))return -2;
        if(await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GUnitNickName WHERE NickName=$name);",ct,("$name",newNickname)))return -3;
        if(await ExistsAsync("SELECT EXISTS(SELECT 1 FROM GDeletedNickName WHERE Nickname=$name);",ct,("$name",newNickname)))return -4;
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if(await ExecAsync(tx,"UPDATE GUnitNickName SET NickName=$name,RegDate=$now WHERE UnitUID=$unit;",ct,("$name",newNickname),("$now",Format(ToSmallDateTime(DateTime.Now))),("$unit",unitUid))!=1)return await RollbackAsync(tx,-5,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }
        catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }
    private async Task<bool> ExistsAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps)=>Convert.ToInt64(await ScalarAsync(sql,ct,ps).ConfigureAwait(false))!=0;
    private async Task<object?> ScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<int> RollbackAsync(SqliteTransaction tx,int value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime v){var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind);return v.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
