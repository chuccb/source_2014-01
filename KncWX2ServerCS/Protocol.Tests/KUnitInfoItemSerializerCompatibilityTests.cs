using KncWX2Server.Protocol;

static class KUnitInfoItemSerializerCompatibilityTests
{
    public static void Run()
    {
        WritesNativeItemOrderWith2013Features();
        WritesNativeItemOrderWithout2013Features();
    }

    private static void WritesNativeItemOrderWith2013Features()
    {
        var item = new KItemInfo
        {
            ItemId = 100,
            UsageType = 2,
            Quantity = 3,
            Endurance = 4,
            SealData = 5,
            EnchantLevel = 6,
            Period = 7,
            ExpirationDate = "expire",
            ItemState = 8,
            GoldTicketKeyUid = 9,
        };

        item.AttributeEnchantInfo.AttribEnchant0 = 10;
        item.AttributeEnchantInfo.AttribEnchant1 = 11;
        item.AttributeEnchantInfo.AttribEnchant2 = 12;
        item.ItemSocket.Add(13);
        item.RandomSocket.Add(14);

        var options = new KUnitInfoWireOptions
        {
            NewItemSystem201305 = true,
            GoldTicket = true,
        };

        var serializer = BeginReading(Serialize(item, options));
        Assert(serializer.Get(out int itemId) && itemId == 100);
        Assert(serializer.Get(out sbyte usageType) && usageType == 2);
        Assert(serializer.Get(out int quantity) && quantity == 3);
        Assert(serializer.Get(out short endurance) && endurance == 4);
        Assert(serializer.Get(out byte sealData) && sealData == 5);
        Assert(serializer.Get(out sbyte enchantLevel) && enchantLevel == 6);
        Assert(serializer.Get(out sbyte attrib0) && attrib0 == 10);
        Assert(serializer.Get(out sbyte attrib1) && attrib1 == 11);
        Assert(serializer.Get(out sbyte attrib2) && attrib2 == 12);

        var sockets = new List<int>();
        var randomSockets = new List<int>();
        Assert(serializer.GetVector(sockets, static s => ReadShort(s)));
        Assert(serializer.GetVector(randomSockets, static s => ReadInt(s)));
        Assert(sockets.SequenceEqual([13]));
        Assert(randomSockets.SequenceEqual([14]));

        Assert(serializer.Get(out sbyte itemState) && itemState == 8);
        Assert(serializer.Get(out short period) && period == 7);
        Assert(serializer.GetW(out string expirationDate) && expirationDate == "expire");
        Assert(serializer.Get(out long goldTicketUid) && goldTicketUid == 9);
        serializer.EndReading();
    }

    private static void WritesNativeItemOrderWithout2013Features()
    {
        var item = new KItemInfo
        {
            ItemId = 200,
            UsageType = 1,
            Quantity = 2,
            Endurance = 3,
            SealData = 4,
            EnchantLevel = 5,
            Period = 6,
            ExpirationDate = "legacy",
        };

        item.ItemSocket.Add(7);

        var options = new KUnitInfoWireOptions
        {
            ItemOptionDataSize = true,
        };

        var serializer = BeginReading(Serialize(item, options));
        Assert(serializer.Get(out int itemId) && itemId == 200);
        Assert(serializer.Get(out sbyte usageType) && usageType == 1);
        Assert(serializer.Get(out int quantity) && quantity == 2);
        Assert(serializer.Get(out short endurance) && endurance == 3);
        Assert(serializer.Get(out byte sealData) && sealData == 4);
        Assert(serializer.Get(out sbyte enchantLevel) && enchantLevel == 5);
        Assert(serializer.Get(out sbyte _));
        Assert(serializer.Get(out sbyte _));
        Assert(serializer.Get(out sbyte _));

        var sockets = new List<int>();
        Assert(serializer.GetVector(sockets, static s => ReadInt(s)));
        Assert(sockets.SequenceEqual([7]));
        Assert(serializer.Get(out short period) && period == 6);
        Assert(serializer.GetW(out string expirationDate) && expirationDate == "legacy");
        serializer.EndReading();
    }

    private static KSerBuffer Serialize(KItemInfo item, KUnitInfoWireOptions options)
    {
        var buffer = new KSerBuffer();
        var serializer = new KSerializer();
        serializer.BeginWriting(buffer);
        Assert(serializer.Put(item, options));
        serializer.EndWriting();
        return buffer;
    }

    private static KSerializer BeginReading(KSerBuffer buffer)
    {
        buffer.Reset();
        var serializer = new KSerializer();
        serializer.BeginReading(buffer);
        return serializer;
    }

    private static (bool Ok, int Value) ReadShort(KSerializer serializer) =>
        serializer.Get(out short value) ? (true, value) : (false, default);

    private static (bool Ok, int Value) ReadInt(KSerializer serializer) =>
        serializer.Get(out int value) ? (true, value) : (false, default);

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("KItemInfo serializer compatibility assertion failed");
        }
    }
}
