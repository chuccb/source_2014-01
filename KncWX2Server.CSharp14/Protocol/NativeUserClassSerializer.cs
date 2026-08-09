namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Adapter for the native SERIALIZE_DEFINE_TAG / SERIALIZE_DEFINE_PUT / GET
/// user-class convention. The native KSerializer writes eTAG_USERCLASS before
/// invoking SerializeHelper::PutInto/GetFrom and propagates the helper's bool.
/// </summary>
public sealed class NativeUserClassSerializer(NativePrimitiveSerializer serializer)
{
    private readonly NativePrimitiveSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public bool Put<T>(T value, Func<NativePrimitiveSerializer, T, bool> putInto)
    {
        ArgumentNullException.ThrowIfNull(putInto);
        _serializer.WriteCollectionTag(NativePrimitiveSerializer.TagUserClass);
        return putInto(_serializer, value);
    }

    public bool TryGet<T>(out T value, Func<NativePrimitiveSerializer, (bool Ok, T Value)> getFrom)
    {
        ArgumentNullException.ThrowIfNull(getFrom);
        value = default!;

        if (!_serializer.ReadCollectionTag(NativePrimitiveSerializer.TagUserClass))
            return false;

        var result = getFrom(_serializer);
        if (!result.Ok)
            return false;

        value = result.Value;
        return true;
    }
}
