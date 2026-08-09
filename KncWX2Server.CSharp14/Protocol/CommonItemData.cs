namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Managed counterpart of native KItemAttributeEnchantInfo.</summary>
public sealed class KItemAttributeEnchantInfo
{
    public sbyte AttribEnchant0 { get; set; }
    public sbyte AttribEnchant1 { get; set; }
    public sbyte AttribEnchant2 { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.AttribEnchant0);
            ser.Put(value.AttribEnchant1);
            ser.Put(value.AttribEnchant2);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KItemAttributeEnchantInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out sbyte a0) || !ser.TryGet(out sbyte a1) || !ser.TryGet(out sbyte a2))
                return (false, existing);
            existing.AttribEnchant0 = a0;
            existing.AttribEnchant1 = a1;
            existing.AttribEnchant2 = a2;
            return (true, existing);
        });
    }
}
