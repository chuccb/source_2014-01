using KncWX2Server.Protocol;

static class KUnitInfoSerializerCompatibilityTests
{
    public static void Run()
    {
        WritesEquippedSkillsInNativeOrder();
        RoundTripsKStat();
    }

    private static void WritesEquippedSkillsInNativeOrder()
    {
        var value = new KUnitSkillData();
        for (var index = 0; index < KUnitSkillData.EquippedSkillSlotCount; index++)
        {
            value.EquippedSkill[index].SkillId = (short)(index + 1);
            value.EquippedSkill[index].SkillLevel = (byte)(index + 21);
            value.EquippedSkillSlotB[index].SkillId = (short)(index + 11);
            value.EquippedSkillSlotB[index].SkillLevel = (byte)(index + 31);
        }

        var buffer = new KSerBuffer();
        var serializer = new KSerializer();
        serializer.BeginWriting(buffer);
        Assert(serializer.Put(value, KUnitInfoWireOptions.Default));
        serializer.EndWriting();

        byte[] expected =
        [
            0, 1, 21,
            0, 2, 22,
            0, 3, 23,
            0, 4, 24,
            0, 11, 31,
            0, 12, 32,
            0, 13, 33,
            0, 14, 34,
            0, 0, 0, 0,
            0,
            0, 0, 0, 0,
        ];

        Assert(buffer.Data.Span.SequenceEqual(expected));
    }

    private static void RoundTripsKStat()
    {
        var source = new KStat
        {
            BaseHp = 11,
            AtkPhysic = 22,
            AtkMagic = 33,
            DefPhysic = 44,
            DefMagic = 55,
        };

        var buffer = new KSerBuffer();
        var writer = new KSerializer();
        writer.BeginWriting(buffer);
        Assert(writer.Put(source));
        writer.EndWriting();
        buffer.Reset();

        var result = new KStat();
        var reader = new KSerializer();
        reader.BeginReading(buffer);
        Assert(reader.Get(result));
        reader.EndReading();

        Assert(result.BaseHp == 11);
        Assert(result.AtkPhysic == 22);
        Assert(result.AtkMagic == 33);
        Assert(result.DefPhysic == 44);
        Assert(result.DefMagic == 55);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("KUnitInfo serializer compatibility assertion failed");
        }
    }
}