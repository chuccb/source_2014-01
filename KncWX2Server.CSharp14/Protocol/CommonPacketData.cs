namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Managed counterpart of native DECL_DATA(KServerSetData).</summary>
public sealed class KServerSetData
{
    public int ServerSetId { get; set; }
    public string ServerSetName { get; set; } = string.Empty;
    public sbyte UserCountLevel { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        serializer.PutWString(ServerSetName);
        serializer.Put(ServerSetId);
        serializer.Put(UserCountLevel);
        return true;
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KServerSetData value)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        value = new KServerSetData();

        if (!serializer.TryGet(out int serverSetId) ||
            !serializer.TryGetWString(out string serverSetName) ||
            !serializer.TryGet(out sbyte userCountLevel))
            return false;

        value.ServerSetId = serverSetId;
        value.ServerSetName = serverSetName;
        value.UserCountLevel = userCountLevel;
        return true;
    }

    public const sbyte UclInvalid = 0;
    public const sbyte UclFree = 1;
    public const sbyte UclNormal = 2;
    public const sbyte UclBusy = 3;
    public const sbyte UclFull = 4;
}
