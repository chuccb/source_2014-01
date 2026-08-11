namespace KncWX2Server.Runtime.Center;

/// <summary>Native KBadAttitudeManager parity port.</summary>
public sealed class BadAttitudeManager
{
    public sealed class SubStageRankInfo
    {
        public int Stage { get; internal set; } = -1;
        public int SubStage { get; internal set; } = -1;
        public char Rank { get; internal set; }
    }

    public sealed class BadAttitudeInfo
    {
        public BadAttitudeInfo(long unitUid) => UnitUid = unitUid;

        public long UnitUid { get; }
        public SubStageRankInfo RankInfo { get; } = new();
        public int BadAttitudePoint { get; internal set; }
        public int VotePoint { get; internal set; }
        public HashSet<long> VotedUnitUids { get; } = [];
        public bool IsBadAttitudeUnit { get; internal set; }
        public bool ForceExit { get; internal set; }
    }

    public delegate char RankEvaluator(int dungeonIdAndDiff, int endNumMember, int value, int monsterPoint);

    private readonly Dictionary<long, BadAttitudeInfo> _unitInfoList = [];
    private readonly Dictionary<(int Stage, int SubStage), int> _subStageMonsterScore = [];
    private readonly KBadAttitudeTable _table;
    private int _dungeonType;
    private int _waveId = 1;

    public BadAttitudeManager(KBadAttitudeTable? table = null) => _table = table ?? new KBadAttitudeTable();

    public int UnitCount => _unitInfoList.Count;
    public int WaveId => _waveId;
    public IReadOnlyDictionary<long, BadAttitudeInfo> UnitInfoList => _unitInfoList;

    public void Init(IEnumerable<long> unitUids, int dungeonType)
    {
        ArgumentNullException.ThrowIfNull(unitUids);

        _unitInfoList.Clear();
        _subStageMonsterScore.Clear();
        _dungeonType = dungeonType;
        _waveId = 1;

        foreach (var unitUid in unitUids)
        {
            _unitInfoList.TryAdd(unitUid, new BadAttitudeInfo(unitUid));
        }
    }

    public bool RemoveUnit(long unitUid, out BadAttitudeInfo? unitData)
    {
        if (!_unitInfoList.Remove(unitUid, out unitData))
        {
            return false;
        }

        foreach (var info in _unitInfoList.Values)
        {
            if (info.VotedUnitUids.Remove(unitUid))
            {
                info.VotePoint = Math.Max(0, info.VotePoint - 1);
            }
        }

        return true;
    }

    public bool IncreaseBadAttitudeOnePoint(long unitUid)
    {
        if (!_unitInfoList.TryGetValue(unitUid, out var info))
        {
            return false;
        }

        info.BadAttitudePoint++;
        return true;
    }

    public bool IncreaseVoteOnePoint(long badAttitudeUnitUid, long voteUnitUid)
    {
        if (!_unitInfoList.ContainsKey(voteUnitUid) || !_unitInfoList.TryGetValue(badAttitudeUnitUid, out var target))
        {
            return false;
        }

        if (!target.VotedUnitUids.Add(voteUnitUid))
        {
            return false;
        }

        target.VotePoint++;
        return true;
    }

    public void CheckBadAttitudeUnit(List<long> newBadAttitudeUnits, List<long> newForceExitUnits)
    {
        ArgumentNullException.ThrowIfNull(newBadAttitudeUnits);
        ArgumentNullException.ThrowIfNull(newForceExitUnits);
        newBadAttitudeUnits.Clear();
        newForceExitUnits.Clear();

        var badAttitudeCutLinePoint = _table.GetBadAttitudeCutLinePoint(_dungeonType);
        var forceExitPoint = _table.GetForceExitPoint(_dungeonType);
        if (badAttitudeCutLinePoint <= 0 || forceExitPoint <= 0)
        {
            return;
        }

        foreach (var info in _unitInfoList.Values)
        {
            if (info.BadAttitudePoint >= badAttitudeCutLinePoint && !info.IsBadAttitudeUnit)
            {
                info.IsBadAttitudeUnit = true;
                newBadAttitudeUnits.Add(info.UnitUid);
            }
        }

        foreach (var unitUid in newBadAttitudeUnits)
        {
            BadAttitudeForceVote(unitUid);
        }

        var voteUnitCountHalf = _unitInfoList.Count switch
        {
            2 or 3 => 1,
            4 => 2,
            _ => 100,
        };

        foreach (var info in _unitInfoList.Values)
        {
            if (!info.IsBadAttitudeUnit || info.VotePoint < voteUnitCountHalf)
            {
                continue;
            }

            if (info.BadAttitudePoint >= badAttitudeCutLinePoint + forceExitPoint)
            {
                info.ForceExit = true;
                newForceExitUnits.Add(info.UnitUid);
            }
        }
    }

    public void BadAttitudeForceVote(long badAttitudeUnitUid)
    {
        if (!_unitInfoList.ContainsKey(badAttitudeUnitUid))
        {
            return;
        }

        foreach (var info in _unitInfoList.Values)
        {
            if (info.UnitUid != badAttitudeUnitUid && info.IsBadAttitudeUnit)
            {
                IncreaseVoteOnePoint(info.UnitUid, badAttitudeUnitUid);
            }
        }
    }

    public bool IsAllUnitGetScore()
    {
        if (_unitInfoList.Count == 0)
        {
            return true;
        }

        var first = true;
        var stage = -1;
        var subStage = -1;

        foreach (var info in _unitInfoList.Values)
        {
            if (first)
            {
                stage = info.RankInfo.Stage;
                subStage = info.RankInfo.SubStage;
                first = false;
                continue;
            }

            if (stage != info.RankInfo.Stage || subStage != info.RankInfo.SubStage)
            {
                return false;
            }
        }

        return true;
    }

    public bool SetUnitSubStageInfo(
        long unitUid,
        int stage,
        int subStage,
        int dungeonIdAndDiff,
        int endNumMember,
        int combo,
        int tech,
        RankEvaluator comboRankEvaluator,
        RankEvaluator techRankEvaluator)
    {
        ArgumentNullException.ThrowIfNull(comboRankEvaluator);
        ArgumentNullException.ThrowIfNull(techRankEvaluator);

        if (stage < 0 || subStage < 0 || !_unitInfoList.TryGetValue(unitUid, out var info))
        {
            return false;
        }

        if (stage < info.RankInfo.Stage && subStage < info.RankInfo.SubStage)
        {
            return false;
        }

        var monsterPoint = _subStageMonsterScore.GetValueOrDefault((stage, subStage));
        var comboRank = comboRankEvaluator(dungeonIdAndDiff, endNumMember, combo, monsterPoint);
        var techRank = techRankEvaluator(dungeonIdAndDiff, endNumMember, tech, monsterPoint);

        info.RankInfo.Stage = stage;
        info.RankInfo.SubStage = subStage;
        info.RankInfo.Rank = comboRank > techRank ? comboRank : techRank;

        if (info.RankInfo.Rank <= 'F' && monsterPoint > 0)
        {
            info.BadAttitudePoint++;
        }

        return true;
    }

    public void IncreaseSubStageMonsterDieCount(int stageId, int subStageId, char monsterTypeFactor, bool isDefenceDungeon = false)
    {
        var key = (stageId, isDefenceDungeon ? _waveId : subStageId);
        _subStageMonsterScore[key] = _subStageMonsterScore.GetValueOrDefault(key) + monsterTypeFactor;
    }

    public void SetDefenceDungeonWaveId(int waveId) => _waveId = waveId;

    public bool GetUnitData(long unitUid, out BadAttitudeInfo? unitData) =>
        _unitInfoList.TryGetValue(unitUid, out unitData);
}
