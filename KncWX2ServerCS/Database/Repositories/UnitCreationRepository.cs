using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record CreateUnitResult(int Code, long UnitUid, DateTime NicknameAvailableAt);

public sealed class UnitCreationRepository
{
    private readonly SqliteDatabase _database;

    public UnitCreationRepository(SqliteDatabase database) => _database = database;

    public async Task<CreateUnitResult> CreateAsync(long userUid,string nickname,byte unitClass,CancellationToken cancellationToken=default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        if(nickname.Length>16)throw new ArgumentOutOfRangeException(nameof(nickname));
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var now=ToSmallDateTime(DateTime.Now);
        var startSpirit=await GetStartSpiritAsync(cancellationToken).ConfigureAwait(false);
        var user=await GetUserAsync(userUid,cancellationToken).ConfigureAwait(false);
        if(user is null||user.Value.Deleted)return new(-1,0,new(2000,1,1));
        var count=await ScalarLongAsync("SELECT COUNT(*) FROM GUnit WHERE Deleted=0 AND UserUID=$uid;",cancellationToken,("$uid",userUid));
        if(count>=user.Value.UserSize)return new(-3,0,new(2000,1,1));
        if(await ScalarLongAsync("SELECT EXISTS(SELECT 1 FROM GUnitNickName WHERE NickName=$name);",cancellationToken,("$name",nickname))!=0)return new(-2,0,new(2000,1,1));
        var deletedNickname=await GetDeletedNicknameDateAsync(nickname,cancellationToken).ConfigureAwait(false);
        var availableAt=new DateTime(2000,1,1);
        if(deletedNickname.HasValue){availableAt=deletedNickname.Value.AddDays(14);if(now<availableAt)return new(-222,0,availableAt);}

        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var unitUid=await InsertUnitAsync(tx,userUid,unitClass,now,cancellationToken).ConfigureAwait(false);
            if(unitUid<=0)return await RollbackResultAsync(tx,new(-12,0,availableAt),cancellationToken);
            if(await ExecuteAsync(tx,"INSERT INTO GUnitNickName(UnitUID,NickName,RegDate) VALUES($uid,$name,$now);",cancellationToken,("$uid",unitUid),("$name",nickname),("$now",Format(now)))!=1)return await RollbackResultAsync(tx,new(-13,0,availableAt),cancellationToken);
            for(var q=1;q<=4;q++)if(await ExecuteAsync(tx,"INSERT INTO GDenyOption(UnitUID,QuestionNo,CodeNo) VALUES($uid,$q,1);",cancellationToken,("$uid",unitUid),("$q",q))!=1)return await RollbackResultAsync(tx,new(-14,0,availableAt),cancellationToken);
            if(await ExecuteAsync(tx,"INSERT INTO GQuests(UnitUID,QuestID,SubQuest0,SubQuest1,SubQuest2,SubQuest3,SubQuest4,RegDate) VALUES($uid,13,1,0,0,0,0,$now);",cancellationToken,("$uid",unitUid),("$now",Format(now)))!=1)return await RollbackResultAsync(tx,new(-14,0,availableAt),cancellationToken);
            var skill=unitClass switch{1=>10000,2=>20030,3=>30000,4=>40010,_=>0};
            if(skill!=0){var skillError=unitClass switch{1=>-16,2=>-18,_=>-20};var slotError=unitClass switch{1=>-15,2=>-17,_=>-19};if(await ExecuteAsync(tx,"INSERT INTO GSkill(UnitUID,SkillID,RegDate) VALUES($uid,$skill,$now);",cancellationToken,("$uid",unitUid),("$skill",skill),("$now",Format(now)))!=1)return await RollbackResultAsync(tx,new(skillError,0,availableAt),cancellationToken);if(await ExecuteAsync(tx,"INSERT INTO GSkillSlot(UnitUID,Slot01,Slot02,Slot03) VALUES($uid,$skill,0,0);",cancellationToken,("$uid",unitUid),("$skill",skill))!=1)return await RollbackResultAsync(tx,new(slotError,0,availableAt),cancellationToken);}
            if(await ExecuteAsync(tx,"INSERT INTO GSpirit(UnitUID,Spirit,RegDate) VALUES($uid,$spirit,$now);",cancellationToken,("$uid",unitUid),("$spirit",startSpirit),("$now",Format(now)))!=1)return await RollbackResultAsync(tx,new(-21,0,availableAt),cancellationToken);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);return new(0,unitUid,availableAt);
        }
        catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    private async Task<(bool Deleted,long UserSize)?> GetUserAsync(long userUid,CancellationToken ct){await using var c=_database.Connection.CreateCommand();c.CommandText="SELECT Deleted,USSize FROM GUser WHERE UserUID=$uid LIMIT 1;";c.Parameters.AddWithValue("$uid",userUid);await using var r=await c.ExecuteReaderAsync(ct).ConfigureAwait(false);if(!await r.ReadAsync(ct).ConfigureAwait(false))return null;return(Convert.ToInt64(r.GetValue(0))!=0,r.GetInt64(1));}
    private async Task<short> GetStartSpiritAsync(CancellationToken ct){await using var c=_database.Connection.CreateCommand();c.CommandText="SELECT StartSpirit FROM GResurrectionStoneCnt LIMIT 1;";var v=await c.ExecuteScalarAsync(ct).ConfigureAwait(false);return v is null||v is DBNull?(short)0:Convert.ToInt16(v);}
    private async Task<DateTime?> GetDeletedNicknameDateAsync(string nickname,CancellationToken ct){await using var c=_database.Connection.CreateCommand();c.CommandText="SELECT Regdate FROM GDeletedNickNameHistory WHERE NickName=$name ORDER BY Regdate DESC LIMIT 1;";c.Parameters.AddWithValue("$name",nickname);var v=await c.ExecuteScalarAsync(ct).ConfigureAwait(false);return v is null||v is DBNull?null:DateTime.Parse(Convert.ToString(v)!);}
    private static async Task<long> InsertUnitAsync(SqliteTransaction tx,long userUid,byte unitClass,DateTime now,CancellationToken ct){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO GUnit(UserUID,UnitClass,Exp,Level,GamePoint,VSPoint,VSPointMax,BaseHP,AtkPhysic,AtkMagic,DefPhysic,DefMagic,SPoint,Win,Lose,Seceder,RegDate,DelDate,LastPosition) VALUES($uid,$class,0,1,0,0,0,0,0,0,0,0,1,0,0,0,$now,$now,20000); SELECT last_insert_rowid();";c.Parameters.AddWithValue("$uid",userUid);c.Parameters.AddWithValue("$class",unitClass);c.Parameters.AddWithValue("$now",Format(now));return Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false));}
    private async Task<long> ScalarLongAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false));}
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<CreateUnitResult> RollbackResultAsync(SqliteTransaction tx,CreateUnitResult result,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return result;}
    private static DateTime ToSmallDateTime(DateTime v){var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind);return v.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
