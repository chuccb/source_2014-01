namespace KncWX2Server.CSharp14.Protocol;

public sealed class KTCInfo
{
    public int TcId { get; set; }
    public int DungeonId { get; set; }
    public long RoomUid { get; set; }
    public sbyte RoomType { get; set; }
    public string UdpRelayIp { get; set; } = string.Empty;
    public ushort UdpRelayPort { get; set; }
    public float PlayTime { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, x) =>
        {
            ser.Put(x.TcId); ser.Put(x.DungeonId); ser.Put(x.RoomUid); ser.Put(x.RoomType);
            ser.PutWString(x.UdpRelayIp); ser.Put(x.UdpRelayPort); ser.Put(x.PlayTime); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KTCInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int tcId) || !ser.TryGet(out int dungeonId) || !ser.TryGet(out long roomUid) ||
                !ser.TryGet(out sbyte roomType) || !ser.TryGetWString(out var ip) ||
                !ser.TryGet(out ushort port) || !ser.TryGet(out float playTime)) return (false, x);
            x.TcId = tcId; x.DungeonId = dungeonId; x.RoomUid = roomUid; x.RoomType = roomType;
            x.UdpRelayIp = ip; x.UdpRelayPort = port; x.PlayTime = playTime;
            return (true, x);
        });
    }
}

public sealed class KSubQuestData
{
    public int InInventoryItemCount { get; set; }
    public bool Success { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, x) =>
        {
            ser.Put(x.InInventoryItemCount); ser.Put(x.Success); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KSubQuestData value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int count) || !ser.TryGet(out bool success)) return (false, x);
            x.InInventoryItemCount = count; x.Success = success; return (true, x);
        });
    }
}

public sealed class KPetInfo
{
    public long PetUid { get; set; }
    public int PetId { get; set; }
    public sbyte LegacyPetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public sbyte EvolutionStep { get; set; }
    public short Satiety { get; set; }
    public int Intimacy { get; set; }
    public short Extroversion { get; set; }
    public short Emotion { get; set; }
    public bool AutoFeed { get; set; }
    public string LastFeedDate { get; set; } = string.Empty;
    public string LastSummonDate { get; set; } = string.Empty;
    public string RegDate { get; set; } = string.Empty;
    public bool AutoLooting { get; set; }
    public bool FreeAutoLooting { get; set; }
    public string DestroyDate { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        new NativeUserClassSerializer(serializer).Put(this, (ser, x) =>
        {
            ser.Put(x.PetUid);
            if (options.PetIdDataTypeChange) ser.Put(x.PetId); else ser.Put(x.LegacyPetId);
            ser.PutWString(x.PetName); ser.Put(x.EvolutionStep); ser.Put(x.Satiety); ser.Put(x.Intimacy);
            ser.Put(x.Extroversion); ser.Put(x.Emotion); ser.Put(x.AutoFeed);
            ser.PutWString(x.LastFeedDate); ser.PutWString(x.LastSummonDate); ser.PutWString(x.RegDate);
            ser.Put(x.AutoLooting);
            if (options.FreeAutoLooting) ser.Put(x.FreeAutoLooting);
            if (options.PeriodPet) ser.PutWString(x.DestroyDate);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KPetInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            if (!ser.TryGet(out long uid)) return (false, x);
            x.PetUid = uid;
            if (options.PetIdDataTypeChange)
            {
                if (!ser.TryGet(out int id)) return (false, x);
                x.PetId = id;
            }
            else
            {
                if (!ser.TryGet(out sbyte id)) return (false, x);
                x.LegacyPetId = id;
            }
            if (!ser.TryGetWString(out var name) || !ser.TryGet(out sbyte evolution) ||
                !ser.TryGet(out short satiety) || !ser.TryGet(out int intimacy) ||
                !ser.TryGet(out short extroversion) || !ser.TryGet(out short emotion) ||
                !ser.TryGet(out bool autoFeed) || !ser.TryGetWString(out var lastFeed) ||
                !ser.TryGetWString(out var lastSummon) || !ser.TryGetWString(out var regDate) ||
                !ser.TryGet(out bool autoLooting)) return (false, x);
            x.PetName = name; x.EvolutionStep = evolution; x.Satiety = satiety; x.Intimacy = intimacy;
            x.Extroversion = extroversion; x.Emotion = emotion; x.AutoFeed = autoFeed;
            x.LastFeedDate = lastFeed; x.LastSummonDate = lastSummon; x.RegDate = regDate; x.AutoLooting = autoLooting;
            if (options.FreeAutoLooting && !ser.TryGet(out x.FreeAutoLooting)) return (false, x);
            if (options.PeriodPet && !ser.TryGetWString(out x.DestroyDate)) return (false, x);
            return (true, x);
        });
    }
}
