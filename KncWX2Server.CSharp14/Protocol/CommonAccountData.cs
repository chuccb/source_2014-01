namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Managed counterpart of native DECL_DATA(KAccountOption).</summary>
public sealed class KAccountOption
{
    public bool PlayGuide { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.PlayGuide);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KAccountOption value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out bool playGuide)) return (false, existing);
            existing.PlayGuide = playGuide;
            return (true, existing);
        });
    }
}

/// <summary>Managed counterpart of native DECL_DATA(KAccountBlockInfo).</summary>
public sealed class KAccountBlockInfo
{
    public string EndTime { get; set; } = string.Empty;
    public string BlockReason { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.PutWString(value.EndTime);
            ser.PutWString(value.BlockReason);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KAccountBlockInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGetWString(out var endTime) || !ser.TryGetWString(out var reason)) return (false, existing);
            existing.EndTime = endTime;
            existing.BlockReason = reason;
            return (true, existing);
        });
    }
}
