using System.Globalization;
using System.Threading;

namespace KncWX2Server.Protocol;

/// <summary>
/// Managed counterpart of KncWX2Server/Common/SimObject.h/.cpp.
/// CLR ownership replaces boost::shared_ptr; reference-count introspection is
/// intentionally not exposed because CLR objects do not have a public count.
/// </summary>
public class KSimObject
{
    private static long s_seedNum;
    private string _name;
    private long _uid;

    public KSimObject()
    {
        _uid = 0;
        long sequence = Interlocked.Increment(ref s_seedNum) - 1;
        DateTime now = DateTime.Now;
        _name = string.Create(
            CultureInfo.InvariantCulture,
            $"SOB_{now:MM/dd/yy}_{now:HH:mm:ss}_{sequence:00000000000000000000}");
    }

    public string Name
    {
        get => _name;
        set => _name = value ?? string.Empty;
    }

    public long Uid
    {
        get => _uid;
        set => _uid = value;
    }

    public override string ToString() =>
        $"{Name}";
}
