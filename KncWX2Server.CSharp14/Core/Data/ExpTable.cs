using System.Collections.Concurrent;
using KncWX2Server.Core.Singleton;

namespace KncWX2Server.Core.Data;

/// <summary>
/// Represents experience table data (replaces KExpTable from C++).
/// Uses C# 14 record type for immutable data transfer.
/// </summary>
public record struct ExpData(int NeedExp, int TotalExp);

/// <summary>
/// Manages experience requirements and level progression.
/// Thread-safe singleton for server-wide access.
/// </summary>
public sealed class ExpTable : SingletonBase<ExpTable>, IDisposable
{
    private readonly ConcurrentDictionary<int, ExpData> _expTable;
    private readonly ReaderWriterLockSlim _tableLock;
    private bool _disposed;

    public ExpTable()
    {
        _expTable = new();
        _tableLock = new();
    }

    /// <summary>
    /// Loads experience table from data source.
    /// </summary>
    public async Task LoadFromDatabaseAsync(IExpTableRepository repository)
    {
        try
        {
            var entries = await repository.GetAllAsync();
            _tableLock.EnterWriteLock();
            try
            {
                _expTable.Clear();
                foreach (var entry in entries)
                {
                    _expTable.TryAdd(entry.Key, entry.Value);
                }
            }
            finally
            {
                _tableLock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load experience table.", ex);
        }
    }

    /// <summary>
    /// Gets the experience requirement for a specific level.
    /// </summary>
    public int GetRequiredExperienceForLevel(int level)
    {
        _tableLock.EnterReadLock();
        try
        {
            return _expTable.TryGetValue(level, out var data) ? data.NeedExp : 0;
        }
        finally
        {
            _tableLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the total accumulated experience for a specific level.
    /// </summary>
    public int GetTotalExperienceForLevel(int level)
    {
        _tableLock.EnterReadLock();
        try
        {
            return _expTable.TryGetValue(level, out var data) ? data.TotalExp : 0;
        }
        finally
        {
            _tableLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Checks if a player should level up based on current experience.
    /// Returns the new level or -1 if no level up occurs.
    /// </summary>
    public int CheckLevelUp(int currentLevel, int currentExp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentLevel);
        ArgumentOutOfRangeException.ThrowIfNegative(currentExp);

        _tableLock.EnterReadLock();
        try
        {
            int newLevel = currentLevel;
            int totalExp = GetTotalExperienceForLevel(currentLevel);
            totalExp += currentExp;

            // Check for multiple level ups
            while (_expTable.TryGetValue(newLevel + 1, out var nextLevelData) &&
                   totalExp >= nextLevelData.TotalExp)
            {
                newLevel++;
            }

            return newLevel > currentLevel ? newLevel : -1;
        }
        finally
        {
            _tableLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Adds or updates an experience entry.
    /// </summary>
    public void SetExpData(int level, ExpData data)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfNegative(data.NeedExp);
        ArgumentOutOfRangeException.ThrowIfNegative(data.TotalExp);

        _expTable.AddOrUpdate(level, data, (_, _) => data);
    }

    /// <summary>
    /// Gets all experience data.
    /// </summary>
    public IReadOnlyDictionary<int, ExpData> GetAllData()
    {
        _tableLock.EnterReadLock();
        try
        {
            return _expTable.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        finally
        {
            _tableLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the maximum level in the table.
    /// </summary>
    public int GetMaxLevel()
    {
        _tableLock.EnterReadLock();
        try
        {
            return _expTable.IsEmpty ? 0 : _expTable.Keys.Max();
        }
        finally
        {
            _tableLock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _tableLock?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Repository interface for experience table data access.
/// </summary>
public interface IExpTableRepository
{
    /// <summary>
    /// Retrieves all experience table entries.
    /// </summary>
    Task<Dictionary<int, ExpData>> GetAllAsync();

    /// <summary>
    /// Retrieves experience data for a specific level.
    /// </summary>
    Task<ExpData?> GetByLevelAsync(int level);

    /// <summary>
    /// Saves experience data.
    /// </summary>
    Task SaveAsync(int level, ExpData data);
}
