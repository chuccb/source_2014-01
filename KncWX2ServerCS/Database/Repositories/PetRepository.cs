using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record PetInfo(int PetUid, int PetId, string PetName, byte Promotion, int FullTime);
public sealed record PetExp(int PetUid, byte Promotion, int Exp);
public sealed record PetEquip(int PetUid, int ItemUid, int ItemType);
public sealed record PetItemDefinition(int PetId, byte Promotion, int ItemId, int ItemType, int Factor);

public sealed class PetRepository
{
    private readonly SqliteDatabase _database;
    public PetRepository(SqliteDatabase database) => _database = database;

    public async Task<IReadOnlyList<(int GoodsId,int Kind)>> GetItemListAsync(CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var c=_database.Connection.CreateCommand(); c.CommandText="SELECT GoodsID,Kind FROM GoodsInfoList WHERE Kind BETWEEN 50 AND 53 ORDER BY GoodsID;";
        var r=new List<(int,int)>(); await using var reader=await c.ExecuteReaderAsync(ct).ConfigureAwait(false); while(await reader.ReadAsync(ct).ConfigureAwait(false)) r.Add((reader.GetInt32(0),reader.GetInt32(1))); return r;
    }

    public async Task<IReadOnlyList<PetEquip>> GetEquipAsync(int petUid,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var c=_database.Connection.CreateCommand(); c.CommandText="SELECT PetUID,ItemUID,ItemType FROM GPetEquip WHERE PetUID=$petUid ORDER BY ItemType,ItemUID;"; c.Parameters.AddWithValue("$petUid",petUid);
        var r=new List<PetEquip>(); await using var reader=await c.ExecuteReaderAsync(ct).ConfigureAwait(false); while(await reader.ReadAsync(ct).ConfigureAwait(false))r.Add(new(reader.GetInt32(0),reader.GetInt32(1),reader.GetInt32(2))); return r;
    }

    public async Task<int> PromoteAsync(int petUid,int loginUid,int petId,string petName,byte promotion,CancellationToken ct=default)
    {
        var check=await CheckAsync(petUid,loginUid,petId,ct).ConfigureAwait(false); if(check!=0)return check;
        await _database.OpenAsync(ct).ConfigureAwait(false);
        var current=await ScalarAsync("SELECT PetName || char(0) || Promotion FROM GPet WHERE PetUID=$petUid AND LoginUID=$loginUid AND PetID=$petId;",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId));
        if(current is null)return -5; var parts=current.ToString()!.Split('\0'); var oldPromotion=byte.Parse(parts[1]);
        if(!await ExistsScalarAsync("SELECT EXISTS(SELECT 1 FROM GPetPromotion WHERE Promotion=$promotion);",ct,("$promotion",promotion)))return -6;
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false); try
        {
            if(await ExecAsync(tx,"UPDATE GPet SET PetName=$petName,Promotion=$promotion WHERE PetUID=$petUid AND LoginUID=$loginUid AND PetID=$petId;",ct,("$petName",petName),("$promotion",promotion),("$petUid",petUid),("$loginUid",loginUid),("$petId",petId))!=1)return await RollbackAsync(tx,-11,ct);
            if(await ExecAsync(tx,"INSERT INTO GPetPromotionLog(PetUID,LoginUID,PetID,OldPetName,NewPetName,OldPromotion,NewPromotion,RegDate) VALUES($petUid,$loginUid,$petId,$oldName,$newName,$oldPromotion,$newPromotion,$now);",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId),("$oldName",parts[0]),("$newName",petName),("$oldPromotion",oldPromotion),("$newPromotion",promotion),("$now",Format(ToSmallDateTime(DateTime.Now))))!=1)return await RollbackAsync(tx,-12,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }

    public async Task<IReadOnlyList<PetItemDefinition>> GetItemDefinitionsAsync(CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var c=_database.Connection.CreateCommand(); c.CommandText="SELECT PetID,Promotion,ItemID,ItemType,Factor FROM GPetItem ORDER BY PetID,Promotion,ItemID,ItemType;";
        var r=new List<PetItemDefinition>(); await using var reader=await c.ExecuteReaderAsync(ct).ConfigureAwait(false); while(await reader.ReadAsync(ct).ConfigureAwait(false))r.Add(new(reader.GetInt32(0),Convert.ToByte(reader.GetValue(1)),reader.GetInt32(2),reader.GetInt32(3),reader.GetInt32(4))); return r;
    }

    public async Task<int> CheckAsync(int petUid,int loginUid,int petId,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if(!await ExistsScalarAsync("SELECT EXISTS(SELECT 1 FROM Users WHERE LoginUID=$loginUid);",ct,("$loginUid",loginUid)))return -1;
        if(!await ExistsScalarAsync("SELECT EXISTS(SELECT 1 FROM GoodsInfoList WHERE GoodsID=$petId AND Kind=50);",ct,("$petId",petId)))return -2;
        var login=await ScalarAsync("SELECT Login FROM Users WHERE LoginUID=$loginUid;",ct,("$loginUid",loginUid));
        if(!await ExistsScalarAsync("SELECT EXISTS(SELECT 1 FROM GoodsObjectList WHERE ItemUID=$petUid AND OwnerLogin=$login AND ItemID=$petId);",ct,("$petUid",petUid),("$login",login??""),("$petId",petId)))return -3; return 0;
    }
    public async Task<IReadOnlyList<PetInfo>> GetInfoAsync(int loginUid,CancellationToken ct=default){await _database.OpenAsync(ct).ConfigureAwait(false);await using var c=_database.Connection.CreateCommand();c.CommandText="SELECT PetUID,PetID,PetName,Promotion,FullTime FROM GPet WHERE LoginUID=$loginUid;";c.Parameters.AddWithValue("$loginUid",loginUid);var r=new List<PetInfo>();await using var q=await c.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await q.ReadAsync(ct).ConfigureAwait(false))r.Add(new(q.GetInt32(0),q.GetInt32(1),q.GetString(2),Convert.ToByte(q.GetValue(3)),q.GetInt32(4)));return r;}
    public async Task<IReadOnlyList<PetExp>> GetExpAsync(int loginUid,CancellationToken ct=default){await _database.OpenAsync(ct).ConfigureAwait(false);await using var c=_database.Connection.CreateCommand();c.CommandText="SELECT a.PetUID,a.Promotion,a.Exp FROM GPetExp a WHERE EXISTS(SELECT 1 FROM GPet b WHERE a.PetUID=b.PetUID AND b.LoginUID=$loginUid);";c.Parameters.AddWithValue("$loginUid",loginUid);var r=new List<PetExp>();await using var q=await c.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await q.ReadAsync(ct).ConfigureAwait(false))r.Add(new(q.GetInt32(0),Convert.ToByte(q.GetValue(1)),q.GetInt32(2)));return r;}

    public async Task<int> CreateAsync(int petUid,int loginUid,int petId,string petName,int charType=-1,CancellationToken ct=default){var check=await CheckAsync(petUid,loginUid,petId,ct).ConfigureAwait(false);if(check!=0)return check;await _database.OpenAsync(ct).ConfigureAwait(false);if(await ExistsScalarAsync("SELECT EXISTS(SELECT 1 FROM GPet WHERE PetUID=$petUid AND LoginUID=$loginUid AND PetID=$petId);",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId)))return -5;var login=await ScalarAsync("SELECT Login FROM Users WHERE LoginUID=$loginUid;",ct,("$loginUid",loginUid));if(login is null)return -6;if(charType!=-1&&!await ExistsScalarAsync("SELECT EXISTS(SELECT 1 FROM Characters WHERE Login=$login AND CharType=$charType);",ct,("$login",login),("$charType",charType)))return -6;await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);try{var now=Format(ToSmallDateTime(DateTime.Now));if(await ExecAsync(tx,"INSERT INTO GPet(PetUID,LoginUID,PetID,PetName,Promotion,FullTime,RegDate) SELECT $petUid,$loginUid,$petId,$petName,Promotion,1000,$now FROM GPetPromotion ORDER BY Promotion LIMIT 1;",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId),("$petName",petName),("$now",now))!=1)return await RollbackAsync(tx,-11,ct);if(await ExecAsync(tx,"INSERT INTO GPetExp(PetUID,Promotion,Exp) SELECT $petUid,Promotion,0 FROM GPetPromotion;",ct,("$petUid",petUid))==0)return await RollbackAsync(tx,-12,ct);if(charType!=-1&&await ExecAsync(tx,"INSERT INTO PIGAPetInfoCharacter(PetUID,CharType,Deleted) VALUES($petUid,$charType,0);",ct,("$petUid",petUid),("$charType",charType))!=1)return await RollbackAsync(tx,-13,ct);await tx.CommitAsync(ct).ConfigureAwait(false);return 0;}catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}}
    public async Task<int> RenameAsync(int petUid,int loginUid,int petId,string newName,CancellationToken ct=default){var check=await CheckAsync(petUid,loginUid,petId,ct).ConfigureAwait(false);if(check!=0)return check;await _database.OpenAsync(ct).ConfigureAwait(false);var old=await ScalarAsync("SELECT PetName FROM GPet WHERE PetUID=$petUid AND LoginUID=$loginUid AND PetID=$petId;",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId));if(old is null)return -5;if(string.Equals(old.ToString(),newName,StringComparison.Ordinal))return -6;await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);try{if(await ExecAsync(tx,"UPDATE GPet SET PetName=$newName WHERE PetUID=$petUid AND LoginUID=$loginUid AND PetID=$petId;",ct,("$newName",newName),("$petUid",petUid),("$loginUid",loginUid),("$petId",petId))!=1)return await RollbackAsync(tx,-11,ct);if(await ExecAsync(tx,"INSERT INTO GPetNameLog(PetUID,OldPetName,NewPetName,RegDate) VALUES($petUid,$oldName,$newName,$now);",ct,("$petUid",petUid),("$oldName",old),("$newName",newName),("$now",Format(ToSmallDateTime(DateTime.Now))))!=1)return await RollbackAsync(tx,-12,ct);await tx.CommitAsync(ct).ConfigureAwait(false);return 0;}catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}}
    public async Task<int> RemoveAsync(int petUid,int loginUid,int petId,CancellationToken ct=default){var check=await CheckAsync(petUid,loginUid,petId,ct).ConfigureAwait(false);if(check!=0)return check;await _database.OpenAsync(ct).ConfigureAwait(false);if(!await ExistsScalarAsync("SELECT EXISTS(SELECT 1 FROM GPet WHERE PetUID=$petUid AND LoginUID=$loginUid AND PetID=$petId);",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId)))return -5;await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);try{var rows=await ExecAsync(tx,"INSERT INTO GPetLog(PetUID,LoginUID,PetID,PetName,Promotion,Exp,FullTime,RegDate,DelDate) SELECT a.PetUID,a.LoginUID,a.PetID,a.PetName,a.Promotion,b.Exp,a.FullTime,a.RegDate,$delDate FROM GPet a JOIN GPetExp b ON a.PetUID=b.PetUID AND a.Promotion=b.Promotion WHERE a.PetUID=$petUid AND a.LoginUID=$loginUid AND a.PetID=$petId;",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId),("$delDate",Format(ToSmallDateTime(DateTime.Now))));if(rows!=1)return await RollbackAsync(tx,-11,ct);if(await ExecAsync(tx,"DELETE FROM GPet WHERE PetUID=$petUid AND LoginUID=$loginUid AND PetID=$petId;",ct,("$petUid",petUid),("$loginUid",loginUid),("$petId",petId))!=1)return await RollbackAsync(tx,-12,ct);await tx.CommitAsync(ct).ConfigureAwait(false);return 0;}catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}}
    private async Task<bool> ExistsScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps)=>Convert.ToInt64(await ScalarAsync(sql,ct,ps).ConfigureAwait(false))!=0;
    private async Task<object?> ScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<int> RollbackAsync(SqliteTransaction tx,int v,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return v;}
    private static DateTime ToSmallDateTime(DateTime v){var m=new DateTime(v.Year,v.Month,v.Day,v.Hour,v.Minute,0,v.Kind);return v.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime v)=>v.ToString("yyyy-MM-dd HH:mm");
}
