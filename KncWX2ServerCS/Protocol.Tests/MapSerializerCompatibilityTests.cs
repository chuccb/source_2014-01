using KncWX2Server.Protocol;

static class MapSerializerCompatibilityTests
{
    public static void Run()
    {
        WritesNativeMapLayoutWithTags();
        WritesNativeMapLayoutWithoutTags();
        ReadsNativeMapLayout();
    }

    private static void WritesNativeMapLayoutWithTags()
    {
        var values = CreateValues();
        var buffer = new KSerBuffer();
        var serializer = new KSerializer();

        serializer.BeginWriting(buffer, tagging: true);
        Assert(serializer.PutMap(values, static (s, value) => s.Put(value), static (s, value) => s.Put(value)));
        serializer.EndWriting();

        byte[] expected =
        [
            22, // SerializeTag.Map
            0, 0, 0, 2,
            16, // SerializeTag.Pair
            5, 0, 0, 0, 1,
            5, 0, 0, 0, 2,
            16, // SerializeTag.Pair
            5, 0, 0, 0, 10,
            5, 0, 0, 0, 20,
        ];

        Assert(buffer.Data.Span.SequenceEqual(expected));
    }

    private static void WritesNativeMapLayoutWithoutTags()
    {
        var values = CreateValues();
        var buffer = new KSerBuffer();
        var serializer = new KSerializer();

        serializer.BeginWriting(buffer);
        Assert(serializer.PutMap(values, static (s, value) => s.Put(value), static (s, value) => s.Put(value)));
        serializer.EndWriting();

        byte[] expected =
        [
            0, 0, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 10,
            0, 0, 0, 20,
        ];

        Assert(buffer.Data.Span.SequenceEqual(expected));
    }

    private static void ReadsNativeMapLayout()
    {
        byte[] data =
        [
            22, // SerializeTag.Map
            0, 0, 0, 2,
            16, // SerializeTag.Pair
            5, 0, 0, 0, 1,
            5, 0, 0, 0, 2,
            16, // SerializeTag.Pair
            5, 0, 0, 0, 10,
            5, 0, 0, 0, 20,
        ];

        var buffer = new KSerBuffer();
        Assert(buffer.Write(data));
        buffer.Reset();

        var values = new Dictionary<int, int>
        {
            [99] = 99,
        };

        var serializer = new KSerializer();
        serializer.BeginReading(buffer, tagging: true);
        Assert(serializer.GetMap(values, ReadInt, ReadInt));
        serializer.EndReading();

        Assert(values.Count == 2);
        Assert(values[1] == 2);
        Assert(values[10] == 20);
    }

    private static SortedDictionary<int, int> CreateValues() =>
        new()
        {
            [10] = 20,
            [1] = 2,
        };

    private static (bool Ok, int Value) ReadInt(KSerializer serializer) =>
        serializer.Get(out int value) ? (true, value) : (false, default);

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("map serializer compatibility assertion failed");
        }
    }
}
