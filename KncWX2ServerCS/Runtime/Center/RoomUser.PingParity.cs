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

        return (uint)_pingScores.Average(static score => score);
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