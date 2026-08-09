namespace KncWX2Server.CSharp14.Protocol;

public sealed class KRankerInfo
{
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.UnitUid);
            ser.PutWString(value.NickName);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KRankerInfo value)
    {
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out long unitUid) || !ser.TryGetWString(out string nickName))
                return (false, x);

            x.UnitUid = unitUid;
            x.NickName = nickName;
            return (true, x);
        });
    }
}

public sealed class KHenirRankingInfo
{
    public int Rank { get; set; }
    public int StageCount { get; set; }
    public uint PlayTime { get; set; }
    public long RegDate { get; set; }
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;
    public sbyte UnitClass { get; set; }
    public byte Level { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Rank);
            ser.Put(value.StageCount);
            ser.Put(value.PlayTime);
            ser.Put(value.RegDate);
            ser.Put(value.UnitUid);
            ser.PutWString(value.NickName);
            ser.Put(value.UnitClass);
            ser.Put(value.Level);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KHenirRankingInfo value)
    {
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int rank) ||
                !ser.TryGet(out int stageCount) ||
                !ser.TryGet(out uint playTime) ||
                !ser.TryGet(out long regDate) ||
                !ser.TryGet(out long unitUid) ||
                !ser.TryGetWString(out string nickName) ||
                !ser.TryGet(out sbyte unitClass) ||
                !ser.TryGet(out byte level))
            {
                return (false, x);
            }

            x.Rank = rank;
            x.StageCount = stageCount;
            x.PlayTime = playTime;
            x.RegDate = regDate;
            x.UnitUid = unitUid;
            x.NickName = nickName;
            x.UnitClass = unitClass;
            x.Level = level;
            return (true, x);
        });
    }
}

public sealed class KDungeonRankingInfo
{
    public int Rank { get; set; }
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;
    public sbyte UnitClass { get; set; }
    public byte Level { get; set; }
    public int Exp { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Rank);
            ser.Put(value.UnitUid);
            ser.PutWString(value.NickName);
            ser.Put(value.UnitClass);
            ser.Put(value.Level);
            ser.Put(value.Exp);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KDungeonRankingInfo value)
    {
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int rank) ||
                !ser.TryGet(out long unitUid) ||
                !ser.TryGetWString(out string nickName) ||
                !ser.TryGet(out sbyte unitClass) ||
                !ser.TryGet(out byte level) ||
                !ser.TryGet(out int exp))
            {
                return (false, x);
            }

            x.Rank = rank;
            x.UnitUid = unitUid;
            x.NickName = nickName;
            x.UnitClass = unitClass;
            x.Level = level;
            x.Exp = exp;
            return (true, x);
        });
    }
}

public sealed class KPvpRankingInfo
{
    public int Rank { get; set; }
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;
    public sbyte UnitClass { get; set; }
    public byte Level { get; set; }
    public sbyte RankValue { get; set; }
    public int Rating { get; set; }
    public int RPoint { get; set; }
    public sbyte PvpEmblem { get; set; }
    public int Lose { get; set; }
    public int Win { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.Rank);
            ser.Put(value.UnitUid);
            ser.PutWString(value.NickName);
            ser.Put(value.UnitClass);
            ser.Put(value.Level);

            if (options.PvpNewSystem)
            {
                if (options.PvpSeason2)
                    ser.Put(value.RankValue);
                else
                    ser.Put(value.Rating);

                ser.Put(value.RPoint);
            }
            else
            {
                ser.Put(value.PvpEmblem);
                ser.Put(value.Lose);
            }

            ser.Put(value.Win);
            return true;
        });

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options,
        out KPvpRankingInfo value)
    {
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            if (!ser.TryGet(out int rank) ||
                !ser.TryGet(out long unitUid) ||
                !ser.TryGetWString(out string nickName) ||
                !ser.TryGet(out sbyte unitClass) ||
                !ser.TryGet(out byte level))
            {
                return (false, x);
            }

            x.Rank = rank;
            x.UnitUid = unitUid;
            x.NickName = nickName;
            x.UnitClass = unitClass;
            x.Level = level;

            if (options.PvpNewSystem)
            {
                if (options.PvpSeason2)
                {
                    if (!ser.TryGet(out sbyte rankValue))
                        return (false, x);

                    x.RankValue = rankValue;
                }
                else
                {
                    if (!ser.TryGet(out int rating))
                        return (false, x);

                    x.Rating = rating;
                }

                if (!ser.TryGet(out int rPoint))
                    return (false, x);

                x.RPoint = rPoint;
            }
            else
            {
                if (!ser.TryGet(out sbyte pvpEmblem) ||
                    !ser.TryGet(out int lose))
                {
                    return (false, x);
                }

                x.PvpEmblem = pvpEmblem;
                x.Lose = lose;
            }

            if (!ser.TryGet(out int win))
                return (false, x);

            x.Win = win;
            return (true, x);
        });
    }
}
