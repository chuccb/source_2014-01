namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Henir reward-count notification payload.</summary>
public sealed class KEgsHenirRewardCountNot
{
    public bool Unlimited { get; set; }
    public int Normal { get; set; }
    public int Premium { get; set; }
    public int Event { get; set; }
    public int PremiumMax { get; set; }
    public int EventMax { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Unlimited);
            ser.Put(value.Normal);
            ser.Put(value.Premium);
            ser.Put(value.Event);
            ser.Put(value.PremiumMax);
            ser.Put(value.EventMax);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEgsHenirRewardCountNot value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out bool unlimited) ||
                !ser.TryGet(out int normal) ||
                !ser.TryGet(out int premium) ||
                !ser.TryGet(out int @event) ||
                !ser.TryGet(out int premiumMax) ||
                !ser.TryGet(out int eventMax))
                return (false, x);

            x.Unlimited = unlimited;
            x.Normal = normal;
            x.Premium = premium;
            x.Event = @event;
            x.PremiumMax = premiumMax;
            x.EventMax = eventMax;
            return (true, x);
        });
    }
}
