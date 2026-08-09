namespace KncWX2Server.Runtime.Center;

public sealed partial class RoomUser
{
    private const uint MissingPingScore = 99_999;
    private readonly List<uint> _pingScores = [];

    public void SetPingScore(uint pingScore)
    {
        ReceivedPingScore = true;
        _pingScores.Add(pingScore);
    }

    public uint GetPingScore()
    {
        if (!ReceivedPingScore || _pingScores.Count == 0)
        {
            return MissingPingScore;
        }

        uint total = 0;
        foreach (var score in _pingScores)
        {
            total = unchecked(total + score);
        }

        return total / (uint)_pingScores.Count;
    }

    public void ClearPingScore()
    {
        if (!ReceivedPingScore || _pingScores.Count == 0)
        {
            return;
        }

        var average = GetPingScore();
        _pingScores.Clear();
        _pingScores.Add(average);
    }

    public void SetPingScoreForForceHost(uint pingScore)
    {
        _pingScores.Clear();
        SetPingScore(pingScore);
    }
}
