namespace KncWX2Server.Common;

/// <summary>Managed counterpart of native KLottery.</summary>
public sealed class KLottery
{
    public const int CaseBlank = -1;
    public const int ParamBlank = -2;

    private const double ProbabilityLimit = 100.000001;
    private const int Modulus = 2_147_483_647;
    private const int Multiplier = 48_271;

    private static int _seed = 42;
    private readonly SortedDictionary<int, LotteryCase> _cases = [];
    private double _totalProbability;

    public double TotalProbability => _totalProbability;
    public int CaseCount => _cases.Count;
    public IReadOnlyDictionary<int, LotteryCase> Cases => _cases;

    public readonly record struct LotteryCase(double Probability, int Param1, int Param2);

    public void Clear()
    {
        _cases.Clear();
        _totalProbability = 0;
    }

    public int GetFirstCase() => _cases.Count == 0 ? CaseBlank : _cases.First().Key;

    public bool IsExistCase(int caseId) => _cases.ContainsKey(caseId);

    public bool AddCase(
        int caseId,
        double probability,
        int param1 = ParamBlank,
        int param2 = ParamBlank)
    {
        if (_totalProbability + probability > ProbabilityLimit)
            return false;

        return AddCaseUnchecked(caseId, probability, param1, param2);
    }

    public bool AddCaseIntegerCast(
        int caseId,
        double probability,
        int param1 = ParamBlank,
        int param2 = ParamBlank)
    {
        if ((int)(_totalProbability + probability) > 100)
            return false;

        return AddCaseUnchecked(caseId, probability, param1, param2);
    }

    public int Decision() => Decision(out _);

    public int Decision(out double checkRoulette)
    {
        checkRoulette = NextUnitDouble() * 100.0;

        var accumulated = 0.0;
        foreach (var (caseId, value) in _cases)
        {
            accumulated += value.Probability;
            if (checkRoulette <= accumulated)
                return caseId;
        }

        return CaseBlank;
    }

    public int GetParam1(int caseId) =>
        _cases.TryGetValue(caseId, out var value) ? value.Param1 : ParamBlank;

    public int GetParam2(int caseId) =>
        _cases.TryGetValue(caseId, out var value) ? value.Param2 : ParamBlank;

    public void GetCaseList(ICollection<int> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();
        foreach (var caseId in _cases.Keys)
            destination.Add(caseId);
    }

    public bool AddProbability(double probabilityToAdd)
    {
        if (probabilityToAdd <= 0 ||
            probabilityToAdd * _cases.Count + _totalProbability > 100.0)
        {
            return false;
        }

        _totalProbability = 0;
        foreach (var (caseId, value) in _cases.ToArray())
        {
            var updated = value with { Probability = value.Probability + probabilityToAdd };
            _cases[caseId] = updated;
            _totalProbability += updated.Probability;
        }

        return _totalProbability <= 100.0;
    }

    public bool DeleteProbability(int caseId)
    {
        if (!_cases.Remove(caseId, out var removed))
            return false;

        _totalProbability -= removed.Probability;
        return true;
    }

    /// <summary>
    /// Preserves the native MakeHundredProbabillty() behavior, including its
    /// historical precondition that refuses to run while total probability is below 100.
    /// </summary>
    public bool MakeHundredProbability()
    {
        if (_totalProbability < 100.0 || _cases.Count == 0)
            return false;

        var remainingProbability = 100.0 - _totalProbability;
        _ = remainingProbability / _cases.Count; // preserved native calculation
        return AddProbability(remainingProbability);
    }

    public bool AddMultiProbRate(double rate)
    {
        if (rate <= 0)
            return false;

        if (_totalProbability * rate > 100.0 && _totalProbability > 0)
            rate = 100.0 / _totalProbability;

        _totalProbability = 0;
        foreach (var (caseId, value) in _cases.ToArray())
        {
            var updated = value with { Probability = value.Probability * rate };
            _cases[caseId] = updated;
            _totalProbability += updated.Probability;
        }

        return true;
    }

    public static void Seed(int seed)
    {
        var normalized = seed % Modulus;
        _seed = normalized == 0 ? 1 : Math.Abs(normalized);
    }

    private bool AddCaseUnchecked(int caseId, double probability, int param1, int param2)
    {
        if (_cases.TryGetValue(caseId, out var existing))
        {
            _cases[caseId] = existing with
            {
                Probability = existing.Probability + probability,
                Param1 = param1,
                Param2 = param2,
            };
        }
        else
        {
            _cases.Add(caseId, new LotteryCase(probability, param1, param2));
        }

        _totalProbability += probability;
        return true;
    }

    private static double NextUnitDouble()
    {
        _seed = (int)((long)Multiplier * _seed % Modulus);
        return (double)_seed / Modulus;
    }
}
