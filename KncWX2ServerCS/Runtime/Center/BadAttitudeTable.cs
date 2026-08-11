namespace KncWX2Server.Runtime.Center;

/// <summary>
/// Native KBadAttitudeTable parity port.
///
/// The native table is keyed by CXSLDungeon::DUNGEON_TYPE. The C# runtime
/// deliberately keeps the key as an integer until the dungeon-data subsystem
/// is ported, so this class does not invent a second dungeon-type enum.
/// </summary>
public sealed class KBadAttitudeTable
{
    public const int DefaultPoint = 1000;

    private readonly Dictionary<int, int> _badAttitudeCutLinePoint = [];
    private readonly Dictionary<int, int> _forceExitPoint = [];

    /// <summary>Replace or add the bad-attitude threshold for a dungeon type.</summary>
    public void AddBadAttitudeCutLinePoint(int dungeonType, int point) =>
        _badAttitudeCutLinePoint[dungeonType] = point;

    /// <summary>Replace or add the additional force-exit threshold for a dungeon type.</summary>
    public void AddForceExitPoint(int dungeonType, int point) =>
        _forceExitPoint[dungeonType] = point;

    /// <summary>
    /// Returns the native threshold. Unknown dungeon types intentionally use
    /// the native fallback value of 1000.
    /// </summary>
    public int GetBadAttitudeCutLinePoint(int dungeonType) =>
        _badAttitudeCutLinePoint.GetValueOrDefault(dungeonType, DefaultPoint);

    /// <summary>
    /// Returns the native additional force-exit threshold. Unknown dungeon
    /// types intentionally use the native fallback value of 1000.
    /// </summary>
    public int GetForceExitPoint(int dungeonType) =>
        _forceExitPoint.GetValueOrDefault(dungeonType, DefaultPoint);

    public void Clear()
    {
        _badAttitudeCutLinePoint.Clear();
        _forceExitPoint.Clear();
    }
}
