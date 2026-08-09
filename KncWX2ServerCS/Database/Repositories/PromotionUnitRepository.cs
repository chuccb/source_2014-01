using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record PromotionUnitResult(int Code, long[] UnitUids);

public sealed class PromotionUnitRepository
{
    private readonly SqliteDatabase _database;
    public PromotionUnitRepository(SqliteDatabase database) => _database = database;

    private static readonly (int Class, int[] Items)[] PromotionItems =
    {
        (1,new[]{128000,128001,128002,128003,128004}),
        (2,new[]{128010,128011,128012,128013,128014}),
        (3,new[]{128005,128006,128007,128008,128009}),
        (4,new[]{128072,128073,128074,128075,128076}),
        (5,new[]{130134,130135,130136,130137,130138})
    };

    public async Task<PromotionUnitResult> CreateAsync(long userUid,string userId,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        if(Convert.ToInt64(await ScalarAsync("SELECT EXISTS(SELECT 1 FROM GUser WHERE UserUID=$uid AND Deleted=0);",ct,("$uid",userUid)))==0)
            return new(-1,Array.Empty<long>());
        var names=Enumerable.Range(1,5).Select(i=>$"{userId}{i}").ToArray();
        var placeholders=string.Join(',',names.Select((_,i)=>$"$n{i}"));
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            await using(var check=tx.Connection!.CreateCommand())
            {
                check.Transaction=tx;check.CommandText=$"SELECT COUNT(*) FROM GUnitNickName WHERE NickName IN ({placeholders});";
                for(var i=0;i<names.Length;i++)check.Parameters.AddWithValue($"$n{i}",names[i]);
                if(Convert.ToInt32(await check.ExecuteScalarAsync(ct).ConfigureAwait(false))>0){await tx.RollbackAsync(ct).ConfigureAwait(false);return new(-2,Array.Empty<long>());}
            }
            var units=new List<long>(5);
            foreach(var p in PromotionItems)
            {
                var uid=await InsertUnitAsync(tx,userUid,names[p.Class-1],p.Class,ct);
                if(uid<=0){await tx.RollbackAsync(ct).ConfigureAwait(false);return new(-10,Array.Empty<long>());}
                units.Add(uid);
                foreach(var item in p.Items) await InsertItemAsync(tx,uid,item,ct);
                await InsertDungeonClearAsync(tx,uid,ct);
                await ExecAsync(tx,"UPDATE GUnit SET Exp=6500000,GamePoint=50000000,SPoint=801 WHERE UnitUID=$uid;",ct,("$uid",uid));
                await EquipPromotionItemsAsync(tx,uid,p.Items,ct);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new(0,units.ToArray());
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    private static async Task<long> InsertUnitAsync(SqliteTransaction tx,long userUid,string nickname,int unitClass,CancellationToken ct)
    {
        var now=DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO GUnit(UserUID,UnitClass,Exp,Level,GamePoint,VSPoint,VSPointMax,BaseHP,AtkPhysic,AtkMagic,DefPhysic,DefMagic,SPoint,Win,Lose,Seceder,RegDate,DelDate,LastPosition) VALUES($user,$class,0,1,0,0,0,0,0,0,0,0,1,0,0,0,$now,$now,20000);SELECT last_insert_rowid();";c.Parameters.AddWithValue("$user",userUid);c.Parameters.AddWithValue("$class",unitClass);c.Parameters.AddWithValue("$now",now);var uid=Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false));
        await using var n=tx.Connection!.CreateCommand();n.Transaction=tx;n.CommandText="INSERT INTO GUnitNickName(UnitUID,NickName,RegDate) VALUES($uid,$name,$now);";n.Parameters.AddWithValue("$uid",uid);n.Parameters.AddWithValue("$name",nickname);n.Parameters.AddWithValue("$now",now);await n.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        await ExecAsync(tx,"INSERT INTO GSpirit(unitUID,Spirit,RegDate,Flag) SELECT $uid,StartSpirit,$now,0 FROM GResurrectionStoneCnt LIMIT 1;",ct,("$uid",uid),("$now",now));
        return uid;
    }
    private static async Task InsertItemAsync(SqliteTransaction tx,long uid,int item,CancellationToken ct)=>await ExecAsync(tx,"INSERT INTO GItem(UnitUID,ItemID,InventoryCategory,SlotID,RegDate,DelDate) VALUES($uid,$item,0,0,$now,$now);",ct,("$uid",uid),("$item",item),("$now",DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
    private static async Task InsertDungeonClearAsync(SqliteTransaction tx,long uid,CancellationToken ct)
    {
        const string sql="""
WITH Modes(GameMode) AS (VALUES
(30000),(30001),(30002),(30010),(30011),(30012),(30020),(30021),(30022),(30030),(30031),(30032),(30040),(30041),(30042),(30050),(30051),(30052),(30060),(30061),(30062),(30070),(30071),(30072),(30080),(30081),(30082),(30090),(30091),(30092),(30100),(30101),(30102),(30110),(30111),(30112),(30120),(30121),(30122),(30130),(30131),(30132),(30140),(30141),(30142),(30150),(30151),(30152),(30160),(30161),(30162),(30170),(30171),(30172),(30180),(30181),(30182),(30190),(30191),(30192),(30200),(30201),(30202),(30210),(30211),(30212),(30220),(30221),(30222),(30230),(30231),(30232),(30240),(30241),(30242),(30250),(30251),(30252),(30260),(30261),(30262),(30270),(30271),(30272),(30280),(30281),(30282),(30290),(30291),(30292),(30300),(30301),(30302))
INSERT INTO GDungeonClear(UnitUID,GameMode,MaxScore,MaxTotalRank,RegDate)
SELECT $uid,GameMode,0,0,$now FROM Modes;""";
        await ExecAsync(tx,sql,ct,("$uid",uid),("$now",DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
    }
    private static async Task EquipPromotionItemsAsync(SqliteTransaction tx,long uid,int[] items,CancellationToken ct){for(var i=0;i<items.Length;i++){var slot=i==0?10:(i+1)*2;await ExecAsync(tx,"UPDATE GItem SET InventoryCategory=9,SlotID=$slot WHERE UnitUID=$uid AND ItemID=$item;",ct,("$slot",slot),("$uid",uid),("$item",items[i]));}}
    private async Task<object?> ScalarAsync(string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
}
