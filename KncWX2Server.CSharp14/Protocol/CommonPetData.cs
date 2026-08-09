namespace KncWX2Server.CSharp14.Protocol;

public sealed class KItemPosition
{
    public const int InvalidPetUid = -1;

    public int SlotCategory { get; set; }
    public int SlotId { get; set; }
    public long PetUid { get; set; } = InvalidPetUid;

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        if (!options.PetSystem)
            return false;

        return new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.SlotCategory);
            ser.Put(value.SlotId);
            ser.Put(value.PetUid);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KItemPosition value,
        ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        value = new();
        if (!options.PetSystem)
            return false;

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int slotCategory) || !ser.TryGet(out int slotId) ||
                !ser.TryGet(out long petUid))
                return (false, existing);
            existing.SlotCategory = slotCategory;
            existing.SlotId = slotId;
            existing.PetUid = petUid;
            return (true, existing);
        });
    }
}
