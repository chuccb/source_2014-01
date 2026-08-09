using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record TitleResult(int Code, DateTime EndDate);

public sealed class TitleRepository
{
    private readonly SqliteDatabase _database;
    public TitleRepository(SqliteDatabase database) => _database = database;

    public async Task<TitleResult> InsertAsync(long unitUid, short titleId, short period, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var endDate = ToSmallDateTime(period == 0 ? new DateTime(2040, 12, 31, 23, 59, 0) : DateTime.Now.AddDays(period));
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var exists = Convert.ToInt64(await ScalarAsync(tx, "SELECT COUNT(*) FROM GTitle_Complete WHERE UnitUID=$unitUid AND TitleID=$titleId;", cancellationToken, ("$unitUid", unitUid), ("$titleId", titleId)));
            string sql;
            if (exists == 0)
            {
                sql = "INSERT INTO GTitle_Complete(UnitUID,TitleID,EndDate,IsHang) VALUES($unitUid,$titleId,$endDate,0);";
                if (await ExecuteAsync(tx, sql, cancellationToken, ("$unitUid", unitUid), ("$titleId", titleId), ("$endDate", Format(endDate))) != 1)
                    return await RollbackAsync(tx, new TitleResult(-1, endDate), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                sql = "UPDATE GTitle_Complete SET EndDate=$endDate WHERE UnitUID=$unitUid AND TitleID=$titleId;";
                if (await ExecuteAsync(tx, sql, cancellationToken, ("$unitUid", unitUid), ("$titleId", titleId), ("$endDate", Format(endDate))) != 1)
                    return await RollbackAsync(tx, new TitleResult(-2, endDate), cancellationToken).ConfigureAwait(false);
            }

            await ExecuteAsync(tx, "DELETE FROM GTitle_Mission WHERE UnitUID=$unitUid AND TitleID=$titleId;", cancellationToken, ("$unitUid", unitUid), ("$titleId", titleId));
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(0, endDate);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<object?> ScalarAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime value){var m=new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0,value.Kind);return value.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
