using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed class TutorRepository
{
    private readonly SqliteDatabase _database;
    public TutorRepository(SqliteDatabase database) => _database = database;

    public async Task<int> InsertAsync(long teacherUid, long studentUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        var activeStudents = await CountAsync("SELECT COUNT(*) FROM GTutor WHERE TeacherUID=$teacherUid AND Deleted=0;", cancellationToken, ("$teacherUid", teacherUid));
        if (activeStudents >= 3) return -2;
        var alreadyStudent = await CountAsync("SELECT COUNT(*) FROM GTutor WHERE StudentUID=$studentUid AND Deleted=0;", cancellationToken, ("$studentUid", studentUid));
        if (alreadyStudent > 0) return -3;

        var now = ToSmallDateTime(DateTime.Now);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var rows = await ExecuteAsync(tx,
                "INSERT INTO GTutor(TeacherUID, StudentUID, RegDate, LastDate, DelDate) VALUES($teacherUid,$studentUid,$now,$now,$now);",
                cancellationToken, ("$teacherUid", teacherUid), ("$studentUid", studentUid), ("$now", Format(now)));
            if (rows != 1) return await RollbackAsync(tx, -1, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    private async Task<long> CountAsync(string sql, CancellationToken ct, params (string Name, object Value)[] ps)
    {
        await using var c=_database.Connection.CreateCommand(); c.CommandText=sql; foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);
        return Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params (string Name,object Value)[] ps)
    { await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime value){var m=new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0,value.Kind);return value.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
