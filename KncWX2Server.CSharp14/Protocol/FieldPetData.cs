namespace KncWX2Server.CSharp14.Protocol;

public sealed class KFieldPetInfo
{
    public long PetUid { get; set; }
    public int PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public sbyte EvolutionStep { get; set; }
    public int Intimacy { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.PetUid);
            if (options.PetIdDataTypeChange) ser.Put(value.PetId);
            else ser.Put(checked((sbyte)value.PetId));
            ser.PutWString(value.PetName);
            ser.Put(value.EvolutionStep);
            ser.Put(value.Intimacy);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KFieldPetInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!ser.TryGet(out long uid)) return (false, existing);
            int petId;
            if (options.PetIdDataTypeChange)
            {
                if (!ser.TryGet(out petId)) return (false, existing);
            }
            else
            {
                if (!ser.TryGet(out sbyte legacyPetId)) return (false, existing);
                petId = legacyPetId;
            }
            if (!ser.TryGetWString(out var name) || !ser.TryGet(out sbyte evolutionStep) || !ser.TryGet(out int intimacy))
                return (false, existing);
            existing.PetUid = uid;
            existing.PetId = petId;
            existing.PetName = name;
            existing.EvolutionStep = evolutionStep;
            existing.Intimacy = intimacy;
            return (true, existing);
        });
    }
}
