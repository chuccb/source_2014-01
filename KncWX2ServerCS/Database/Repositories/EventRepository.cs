using Microsoft.Data.Sqlite;

namespace KncWX2Server.Database.Repositories;

public sealed record ThirtyMinuteEvent(byte EventType, DateTime? RegDate);
public sealed record TrainingCenterClear(int Tcid, DateTime RegDate);

public sealed class EventRepository
{
    private readonly SqliteDatabase _database;
    public EventRepository(SqliteDatabase database) => _database = database;

    public async Task<IReadOnlyList<ThirtyMinuteEvent>> Get30MinuteAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT EventType, RegDate FROM GIs30Min WHERE UnitUID=$unitUid;";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        var result = new List<ThirtyMinuteEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            result.Add(new(Convert.ToByte(reader.GetValue(0)), reader.IsDBNull(1) ? null : DateTime.Parse(reader.GetString(1))));
        return result;
    }

    public async Task<int> Update30MinuteAsync(long unitUid, short eventType, DateTime endDate, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var exists = Convert.ToInt64(await ScalarAsync(tx, "SELECT EXISTS(SELECT 1 FROM GIs30Min WHERE UnitUID=$unitUid AND EventType=$eventType);", cancellationToken, ("$unitUid", unitUid), ("$eventType", eventType))) != 0;
            int rows;
            if (exists)
                rows = await ExecuteAsync(tx, "UPDATE GIs30Min SET RegDate=$regDate WHERE UnitUID=$unitUid AND EventType=$eventType;", cancellationToken, ("$regDate", Format(ToSmallDateTime(endDate))), ("$unitUid", unitUid), ("$eventType", eventType));
            else
                rows = await ExecuteAsync(tx, "INSERT INTO GIs30Min(UnitUID, EventType, RegDate) VALUES($unitUid,$eventType,$regDate);", cancellationToken, ("$unitUid", unitUid), ("$eventType", eventType), ("$regDate", Format(ToSmallDateTime(endDate))));
            if (rows > 1) return await RollbackAsync(tx, exists ? -1 : -2, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    public async Task<int> InsertTrainingCenterClearAsync(long unitUid, int tcid, DateTime regDate, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (!await UnitExistsAsync(unitUid, cancellationToken).ConfigureAwait(false)) return -1;
        if (Convert.ToInt64(await ScalarAsync(null, "SELECT EXISTS(SELECT 1 FROM GTrainingCenter WHERE UnitUID=$unitUid AND TCID=$tcid);", cancellationToken, ("$unitUid", unitUid), ("$tcid", tcid), useMainConnection: true)) != 0) return -2;
        await using var tx = (SqliteTransaction)await _database.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var rows = await ExecuteAsync(tx, "INSERT INTO GTrainingCenter(UnitUID, TCID, RegDate) VALUES($unitUid,$tcid,$regDate);", cancellationToken, ("$unitUid", unitUid), ("$tcid", tcid), ("$regDate", Format(ToSmallDateTime(regDate))));
            if (rows != 1) return await RollbackAsync(tx, -3, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
    }

    public async Task<IReadOnlyList<TrainingCenterClear>> GetTrainingCenterClearAsync(long unitUid, CancellationToken cancellationToken = default)
    {
        await _database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = _database.Connection.CreateCommand();
        command.CommandText = "SELECT TCID, RegDate FROM GTrainingCenter WHERE UnitUID=$unitUid;";
        command.Parameters.AddWithValue("$unitUid", unitUid);
        var result = new List<TrainingCenterClear>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) result.Add(new(reader.GetInt32(0), DateTime.Parse(reader.GetString(1))));
        return result;
    }

    private async Task<bool> UnitExistsAsync(long unitUid, CancellationToken ct){await using var c=_database.Connection.CreateCommand();c.CommandText="SELECT EXISTS(SELECT 1 FROM GUnit WHERE UnitUID=$unitUid AND Deleted=0);";c.Parameters.AddWithValue("$unitUid",unitUid);return Convert.ToInt64(await c.ExecuteScalarAsync(ct).ConfigureAwait(false))!=0;}
    private static async Task<object?> ScalarAsync(SqliteTransaction? tx,string sql,CancellationToken ct,params (string Name,object Value)[] ps){await using var c=(tx?.Connection ?? throw new InvalidOperationException()).CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteScalarAsync(ct).ConfigureAwait(false);}
    private static async Task<object?> ScalarAsync(SqliteTransaction? tx,string sql,CancellationToken ct,(string Name,object Value) dummy, bool useMainConnection=false, params (string Name,object Value)[] ps) => null;
    private static async Task<int> ExecuteAsync(SqliteTransaction tx,string sql,CancellationToken ct,params(string Name,object Value)[] ps){await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;c.CommandText=sql;foreach(var p in ps)c.Parameters.AddWithValue(p.Name,p.Value);return await c.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<T> RollbackAsync<T>(SqliteTransaction tx,T value,CancellationToken ct){await tx.RollbackAsync(ct).ConfigureAwait(false);return value;}
    private static DateTime ToSmallDateTime(DateTime value){var m=new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0,value.Kind);return value.Second>=30?m.AddMinutes(1):m;}
    private static string Format(DateTime value)=>value.ToString("yyyy-MM-dd HH:mm");
}
