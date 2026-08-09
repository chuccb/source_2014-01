namespace KncWX2Server.Protocol;

/// <summary>
/// Managed counterpart of X2ServerProtocol/KSimObject.
/// boost::shared_ptr lifetime semantics are represented by normal CLR object
/// ownership; explicit reference counting is intentionally not emulated.
/// </summary>
public abstract class KSimObject
{
    private string _name = string.Empty;
    private long _uid;

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
        $"{GetType().Name}(Name={Name}, Uid={Uid})";
}
