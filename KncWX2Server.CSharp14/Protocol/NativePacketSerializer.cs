namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Small adapter for the native DECL_PACKET/DECL_DATA convention.
/// A packet is a user-defined serializer type (K&lt;EVENT_ID&gt; in native code),
/// so its framing is controlled by NativePrimitiveSerializer's tagging setting.
/// </summary>
public static class NativePacketSerializer
{
    public static bool Put<T>(
        NativePrimitiveSerializer serializer,
        T packet,
        Func<NativePrimitiveSerializer, T, bool> putInto)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(putInto);
        return new NativeUserClassSerializer(serializer).Put(packet, putInto);
    }

    public static bool TryGet<T>(
        NativePrimitiveSerializer serializer,
        out T packet,
        Func<NativePrimitiveSerializer, T, bool> getFrom,
        Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(getFrom);
        ArgumentNullException.ThrowIfNull(factory);

        var existing = factory();
        if (!new NativeUserClassSerializer(serializer).TryGet(
                out T decoded,
                (ser, value) => getFrom(ser, value)
                    ? (true, value)
                    : (false, value)))
        {
            packet = existing;
            return false;
        }

        packet = decoded;
        return true;
    }
}
