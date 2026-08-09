namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Riding-pet state serialized by KRidingPetInfo in CommonPacket.cpp.</summary>
public sealed class KRidingPetInfo
{
    public long RidingPetUid { get; set; }
    public ushort RidingPetId { get; set; }
    public float Stamina { get; set; }
    public string DestroyDate { get; set; } = string.Empty;
    public long LastUnSummonDate { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer)
    {
        return new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.RidingPetUid);
            ser.Put(value.RidingPetId);
            ser.Put(value.Stamina);
            ser.PutWString(value.DestroyDate);
            ser.Put(value.LastUnSummonDate);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KRidingPetInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out long ridingPetUid) ||
                !ser.TryGet(out ushort ridingPetId) ||
                !ser.TryGet(out float stamina) ||
                !ser.TryGetWString(out var destroyDate) ||
                !ser.TryGet(out long lastUnSummonDate))
                return (false, x);

            x.RidingPetUid = ridingPetUid;
            x.RidingPetId = ridingPetId;
            x.Stamina = stamina;
            x.DestroyDate = destroyDate;
            x.LastUnSummonDate = lastUnSummonDate;
            return (true, x);
        });
    }
}
