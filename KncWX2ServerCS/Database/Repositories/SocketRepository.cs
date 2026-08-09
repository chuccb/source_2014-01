using Microsoft.Data.Sqlite;
namespace KncWX2Server.Database.Repositories;
public sealed class SocketRepository
{
    private readonly SqliteDatabase _database;
    public SocketRepository(SqliteDatabase database)=>_database=database;
    public async Task<int> UpdateAsync(long itemUid,short socket1,short socket2,short socket3,short socket4,CancellationToken ct=default)
    {
        await _database.OpenAsync(ct).ConfigureAwait(false);
        await using var tx=(SqliteTransaction)await _database.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if(Convert.ToInt64(await ScalarAsync(tx,"SELECT EXISTS(SELECT 1 FROM GItemSocket WHERE ItemUID=$item);",ct,("$item",itemUid)))==0)
            {
                if(await ExecAsync(tx,"INSERT INTO GItemSocket(ItemUID,Socket1,Socket2,Socket3,Socket4) VALUES($item,$s1,$s2,$s3,$s4);",ct,("$item",itemUid),("$s1",socket1),("$s2",socket2),("$s3",socket3),("$s4",socket4))!=1)return await FailAsync(tx,-1,ct);
            }
            else if(await ExecAsync(tx,"UPDATE GItemSocket SET Socket1=$s1,Socket2=$s2,Socket3=$s3,Socket4=$s4 WHERE ItemUID=$item;",ct,("$item",itemUid),("$s1",socket1),("$s2",socket2),("$s3",socket3),("$s4",socket4))!=1)return await FailAsync(tx,-2,ct);
            await tx.CommitAsync(ct).ConfigureAwait(false);return 0;
        }catch{await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);throw;}
    }
    private static async Task<object?> ScalarAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<int> FailAsync(SqliteTransaction tx,int code,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return code;}
}
