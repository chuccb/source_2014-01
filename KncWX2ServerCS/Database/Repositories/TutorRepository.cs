using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record TutorEntry(long UnitUid, int Level, string? Nickname, DateTime? LastDate);

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
            var rows = await ExecuteAsync(tx, "INSERT INTO GTutor(TeacherUID, StudentUID, RegDate, LastDate, DelDate) VALUES($teacherUid,$studentUid,$now,$now,$now);", cancellationToken, ("$teacherUid", teacherUid), ("$studentUid", studentUid), ("$now", Format(now)));
            if (rows != 1) return await RollbackAsync(tx, -1, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    public async Task<IReadOnlyList<TutorEntry>> GetAsync(bool isTeacher, long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            const string teacherSql = "SELECT a.StudentUID,b.Level,c.Nickname,a.LastDate FROM GTutor a JOIN GUnit b ON a.StudentUID=b.UnitUID LEFT JOIN GUnitNickName c ON a.StudentUID=c.UnitUID WHERE a.TeacherUID=$unitUid AND a.Deleted=0;";
            const string studentSql = "SELECT a.TeacherUID,b.Level,c.Nickname,a.LastDate FROM GTutor a JOIN GUnit b ON a.TeacherUID=b.UnitUID LEFT JOIN GUnitNickName c ON a.TeacherUID=c.UnitUID WHERE a.StudentUID=$unitUid AND a.Deleted=0;";
            var result = new List<TutorEntry>();
            await using (var command = tx.Connection!.CreateCommand())
            {
                command.Transaction = tx; command.CommandText = isTeacher ? teacherSql : studentSql; command.Parameters.AddWithValue("$unitUid", unitUid);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    result.Add(new(reader.GetInt64(0), reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3))));
            }
            if (!isTeacher)
            {
                await using var update = tx.Connection!.CreateCommand(); update.Transaction = tx; update.CommandText="UPDATE GTutor SET LastDate=$now WHERE StudentUID=$unitUid;";
                update.Parameters.AddWithValue("$now", Format(ToSmallDateTime(DateTime.Now))); update.Parameters.AddWithValue("$unitUid", unitUid);
                await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    private async Task<long> CountAsync(string sql,CancellationToken ct,params (string Name,object Value)[] ps){await using var c=_database.Connection.CreateCommand();c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false));}
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params (string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime value){var m=new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0,value.Kind);return value.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
