using System.Runtime.InteropServices;
using System.Text;

namespace KncWX2Server.Protocol;

/// <summary>Compatibility serializer for the native KSerializer wire format.</summary>
/// <remarks>
/// The native implementation serializes numeric values in network byte order, prefixes
/// values with one-byte type tags only when tagging is enabled, and stores string lengths
/// as DWORD byte counts. See KNCSDK/Include/Serializer/Serializer.cpp.
/// </remarks>
public sealed class KSerializer
{
    private KSerBuffer? _buffer;
    private bool _tagsEnabled;
    public bool BeginWriting(KSerBuffer buffer, bool tagging = false) { _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer)); _tagsEnabled = tagging; return true; }
    public bool EndWriting() { _buffer = null; return true; }
    public bool BeginReading(KSerBuffer buffer, bool tagging = false) { _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer)); _tagsEnabled = tagging; return true; }
    public bool EndReading() { _buffer = null; return true; }
    public int ReadLength => _buffer?.ReadLength ?? 0;

    public bool Put(byte v) => WriteNumber(v, SerializeTag.UChar); public bool Get(out byte v) => ReadNumber(out v, SerializeTag.UChar);
    public bool Put(sbyte v) => WriteNumber(unchecked((byte)v), SerializeTag.Char); public bool Get(out sbyte v) { var ok=ReadNumber(out byte x,SerializeTag.Char); v=unchecked((sbyte)x); return ok; }
    public bool Put(char v) => WriteNumber((ushort)v, SerializeTag.WChar); public bool Get(out char v) { var ok=ReadNumber(out ushort x,SerializeTag.WChar); v=(char)x; return ok; }
    public bool Put(short v)=>WriteNumber(v,SerializeTag.Short); public bool Get(out short v)=>ReadNumber(out v,SerializeTag.Short);
    public bool Put(ushort v)=>WriteNumber(v,SerializeTag.UShort); public bool Get(out ushort v)=>ReadNumber(out v,SerializeTag.UShort);
    public bool Put(int v)=>WriteNumber(v,SerializeTag.Int); public bool Get(out int v)=>ReadNumber(out v,SerializeTag.Int);
    public bool Put(uint v)=>WriteNumber(v,SerializeTag.DWord); public bool Get(out uint v)=>ReadNumber(out v,SerializeTag.DWord);
    public bool Put(long v)=>WriteNumber(v,SerializeTag.Int64); public bool Get(out long v)=>ReadNumber(out v,SerializeTag.Int64);
    public bool Put(ulong v)=>WriteNumber(v,SerializeTag.UInt64); public bool Get(out ulong v)=>ReadNumber(out v,SerializeTag.UInt64);
    public bool Put(float v)=>WriteNumber(BitConverter.SingleToInt32Bits(v),SerializeTag.Float); public bool Get(out float v){var ok=ReadNumber(out int x,SerializeTag.Float);v=BitConverter.Int32BitsToSingle(x);return ok;}
    public bool Put(double v)=>WriteNumber(BitConverter.DoubleToInt64Bits(v),SerializeTag.Double); public bool Get(out double v){var ok=ReadNumber(out long x,SerializeTag.Double);v=BitConverter.Int64BitsToDouble(x);return ok;}
    public bool Put(bool v)=>WriteNumber((byte)(v?1:0),SerializeTag.Bool); public bool Get(out bool v){var ok=ReadNumber(out byte x,SerializeTag.Bool);v=x==1;return ok;}

    public bool Put(string value){ArgumentNullException.ThrowIfNull(value);if(!WriteTag(SerializeTag.String))return false;var b=Encoding.UTF8.GetBytes(value);return Put((uint)b.Length)&&(b.Length==0||WriteBytes(b));}
    public bool Get(out string value){value=string.Empty;if(!ReadAndCheckTag(SerializeTag.String)||!Get(out uint n)||n>ReadLength||n>int.MaxValue)return false;if(n==0)return true;var b=new byte[(int)n];if(!ReadBytes(b))return false;value=Encoding.UTF8.GetString(b);return true;}
    public bool PutW(string value)=>PutWide(value);
    public bool GetW(out string value)=>GetWide(out value);
    public bool PutWide(string value){ArgumentNullException.ThrowIfNull(value);if(!WriteTag(SerializeTag.WString))return false;var b=Encoding.Unicode.GetBytes(value);return Put((uint)b.Length)&&(b.Length==0||WriteBytes(b));}
    public bool GetWide(out string value){value=string.Empty;if(!ReadAndCheckTag(SerializeTag.WString)||!Get(out uint n)||(n&1)!=0||n>ReadLength||n>int.MaxValue)return false;if(n==0)return true;var b=new byte[(int)n];if(!ReadBytes(b))return false;value=Encoding.Unicode.GetString(b);return true;}
    public bool PutRaw(ReadOnlySpan<byte> b)=>!b.IsEmpty&&WriteTag(SerializeTag.RawBytes)&&WriteBytes(b);
    public bool GetRaw(Span<byte> b)=>!b.IsEmpty&&ReadAndCheckTag(SerializeTag.RawBytes)&&ReadBytes(b);

    public bool Put(KSerBuffer v){ArgumentNullException.ThrowIfNull(v);if(!WriteTag(SerializeTag.Buffer)||!Put((uint)v.Length))return false;if(v.Length==0)return true;return Put(v.IsCompressed)&&PutRaw(v.Data.Span);}
    public bool Get(KSerBuffer v){ArgumentNullException.ThrowIfNull(v);v.Clear();if(!ReadAndCheckTag(SerializeTag.Buffer)||!Get(out uint n))return false;if(n==0)return true;if(!Get(out bool compressed)||n>ReadLength||n>int.MaxValue)return false;var b=new byte[(int)n];if(!GetRaw(b))return false;v.Write(b);if(compressed)v.MarkCompressed();return true;}

    public bool Put(KPerformerInfo v){ArgumentNullException.ThrowIfNull(v);if(!WriteTag(SerializeTag.UserClass)||!Put(v.PerformerId)||!WriteTag(SerializeTag.Set)||!Put((uint)v.UidListSize))return false;foreach(var uid in v.UidList)if(!Put(uid))return false;return true;}
    public bool Get(KPerformerInfo v){ArgumentNullException.ThrowIfNull(v);if(!ReadAndCheckTag(SerializeTag.UserClass)||!Get(out uint id)||!ReadAndCheckTag(SerializeTag.Set)||!Get(out uint n)||n>KPerformerInfo.MaxUidNum)return false;v.ClearUids();for(uint i=0;i<n;i++){if(!Get(out long uid))return false;v.AddUid(uid);}v.PerformerId=id;return true;}

    /// <summary>Native STL helpers: container tag, DWORD count, then each element.</summary>
    public bool PutVector<T>(IReadOnlyList<T> values, Func<KSerializer,T,bool> put){ArgumentNullException.ThrowIfNull(values);ArgumentNullException.ThrowIfNull(put);if(!WriteTag(SerializeTag.Vector)||!Put((uint)values.Count))return false;foreach(var x in values)if(!put(this,x))return false;return true;}
    public bool GetVector<T>(ICollection<T> values, Func<KSerializer,(bool Ok,T Value)> get){ArgumentNullException.ThrowIfNull(values);ArgumentNullException.ThrowIfNull(get);if(!ReadAndCheckTag(SerializeTag.Vector)||!Get(out uint n))return false;values.Clear();for(uint i=0;i<n;i++){var r=get(this);if(!r.Ok)return false;values.Add(r.Value);}return true;}
    public bool PutList<T>(IReadOnlyList<T> values, Func<KSerializer,T,bool> put)=>PutContainer(SerializeTag.List,values,put);
    public bool PutDeque<T>(IReadOnlyList<T> values, Func<KSerializer,T,bool> put)=>PutContainer(SerializeTag.Deque,values,put);
    public bool PutSet<T>(IEnumerable<T> values, Func<KSerializer,T,bool> put){var a=values.ToArray();return PutContainer(SerializeTag.Set,a,put);}
    public bool PutPair<T1,T2>(T1 a,T2 b,Func<KSerializer,T1,bool> pa,Func<KSerializer,T2,bool> pb)=>WriteTag(SerializeTag.Pair)&&pa(this,a)&&pb(this,b);
    public bool GetPair<T1,T2>(out T1 a,out T2 b,Func<KSerializer,(bool Ok,T1 Value)> ga,Func<KSerializer,(bool Ok,T2 Value)> gb){a=default!;b=default!;if(!ReadAndCheckTag(SerializeTag.Pair))return false;var x=ga(this);if(!x.Ok)return false;var y=gb(this);if(!y.Ok)return false;a=x.Value;b=y.Value;return true;}
    public bool PutEvent(KEvent e){ArgumentNullException.ThrowIfNull(e);return WriteTag(SerializeTag.UserClass)&&Put(e.Destination)&&Put(e.FirstTrace)&&Put(e.LastTrace)&&Put(e.EventId)&&Put(e.Buffer);}
    public bool GetEvent(KEvent e){ArgumentNullException.ThrowIfNull(e);if(!ReadAndCheckTag(SerializeTag.UserClass)||!Get(e.Destination)||!Get(out long first)||!Get(out long last)||!Get(out ushort id)||!Get(e.Buffer))return false;e.SetData(e.Destination.PerformerId,[first,last],id);return true;}

    private bool PutContainer<T>(SerializeTag tag,IReadOnlyList<T> values,Func<KSerializer,T,bool> put){if(!WriteTag(tag)||!Put((uint)values.Count))return false;foreach(var x in values)if(!put(this,x))return false;return true;}
    private bool WriteNumber<T>(T value,SerializeTag tag) where T:unmanaged{if(!WriteTag(tag))return false;int n=Marshal.SizeOf<T>();Span<byte>b=stackalloc byte[8];MemoryMarshal.Write(b,in value);if(BitConverter.IsLittleEndian)b[..n].Reverse();return WriteBytes(b[..n]);}
    private bool ReadNumber<T>(out T value,SerializeTag tag) where T:unmanaged{value=default;if(!ReadAndCheckTag(tag))return false;int n=Marshal.SizeOf<T>();Span<byte>b=stackalloc byte[8];if(!ReadBytes(b[..n]))return false;if(BitConverter.IsLittleEndian)b[..n].Reverse();value=MemoryMarshal.Read<T>(b);return true;}
    private bool WriteTag(SerializeTag tag)=>!_tagsEnabled||WriteBytes([(byte)tag]);
    private bool ReadAndCheckTag(SerializeTag expected){if(!_tagsEnabled)return true;Span<byte>b=stackalloc byte[1];return ReadBytes(b)&&b[0]==(byte)expected;}
    private bool WriteBytes(ReadOnlySpan<byte>b)=>_buffer is not null&&!b.IsEmpty&&_buffer.Write(b);
    private bool ReadBytes(Span<byte>b)=>_buffer is not null&&!b.IsEmpty&&_buffer.Read(b);
}

public enum SerializeTag:byte{Char,WChar,UChar,Short,UShort,Int,DWord,Int64,UInt64,Float,Double,Bool,String,WString,Array,RawBytes,Pair,Vector,List,Deque,Set,Multiset,Map,Multimap,Buffer,KeyedSerializer,UserClass}
