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

        packet = factory();
        return new NativeUserClassSerializer(serializer).TryGet(
            out packet,
            static (ser, value, reader) => reader(ser, value),
            getFrom);
    }

    // Kept separate from the public overload so packet implementations can use
    // a strongly typed object without reflection or dynamic dispatch.
    private static bool TryGet<T>(
        this NativeUserClassSerializer userClass,
        out T value,
        Func<NativePrimitiveSerializer, T, (bool Ok, T Value)> reader,
        Func<NativePrimitiveSerializer, T, bool> getFrom)
    {
        value = default!;
        if (!userClass.TryGet(out T existing, static (ser, packet, state) =>
            state(ser, packet) ? (true, packet) : (false, packet), getFrom))
            return false;

        value = existing;
        return true;
    }
}
