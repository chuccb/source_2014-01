internal static class ByteSpanCompatibilityExtensions
{
    public static bool SequenceEqual(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> other) =>
        System.MemoryExtensions.SequenceEqual(span, other);
}
