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
    public byte BlockType { get; set; }
    public string BlockReason2 { get; set; } = string.Empty;
    public string BlockEndDate { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.PutWString(value.EndTime);
            ser.PutWString(value.BlockReason);
            if (options.HackingUserCheckCount)
            {
                ser.Put(value.BlockType);
                ser.PutWString(value.BlockReason2);
                ser.PutWString(value.BlockEndDate);
            }
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KAccountBlockInfo value, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!ser.TryGetWString(out var endTime) || !ser.TryGetWString(out var reason)) return (false, existing);
            existing.EndTime = endTime;
            existing.BlockReason = reason;
            if (options.HackingUserCheckCount)
            {
                if (!ser.TryGet(out byte blockType) || !ser.TryGetWString(out var reason2) || !ser.TryGetWString(out var endDate))
                    return (false, existing);
                existing.BlockType = blockType;
                existing.BlockReason2 = reason2;
                existing.BlockEndDate = endDate;
            }
            return (true, existing);
        });
    }
}
