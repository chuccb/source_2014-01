namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Managed counterpart of native DECL_DATA(KNetAddress).</summary>
public sealed class KNetAddress
{
    public string Ip { get; set; } = string.Empty;
    public ushort Port { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.PutWString(value.Ip);
            ser.Put(value.Port);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KNetAddress value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(
            out value,
            static (ser, existing) =>
            {
                if (!ser.TryGetWString(out var ip) || !ser.TryGet(out ushort port))
                    return (false, existing);
                existing.Ip = ip;
                existing.Port = port;
                return (true, existing);
            });
    }
}
