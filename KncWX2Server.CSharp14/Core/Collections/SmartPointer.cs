namespace KncWX2Server.Core.Collections;

/// <summary>
/// Replacement for C++ shared_ptr using C# 14.
/// In C#, we rely on garbage collection, but this provides a wrapper
/// for explicit resource management when needed.
/// </summary>
public sealed class SmartPointer<T> : IDisposable where T : class
{
    private T? _value;
    private readonly WeakReference<T>? _weakReference;
    private readonly bool _ownsResource;
    private bool _disposed;

    /// <summary>
    /// Creates a strong reference to an object.
    /// </summary>
    public SmartPointer(T? value, bool ownsResource = true)
    {
        _value = value;
        _ownsResource = ownsResource;
        _weakReference = null;
    }

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public T? Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _value;
        }
    }

    /// <summary>
    /// Attempts to get the value from a weak reference.
    /// </summary>
    public T? GetWeakValue()
    {
        if (_weakReference != null && _weakReference.TryGetTarget(out var target))
        {
            return target;
        }
        return null;
    }

    /// <summary>
    /// Dereference operator equivalent.
    /// </summary>
    public T? Dereference() => Value;

    /// <summary>
    /// Implicit conversion to the underlying type.
    /// </summary>
    public static implicit operator T?(SmartPointer<T> ptr) => ptr.Value;

    /// <summary>
    /// Implicit conversion from the underlying type.
    /// </summary>
    public static implicit operator SmartPointer<T>(T? value) => new(value);

    public void Dispose()
    {
        if (_disposed) return;

        if (_ownsResource && _value is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _value = null;
        _disposed = true;
    }
}

/// <summary>
/// Factory methods for creating SmartPointer instances.
/// </summary>
public static class SmartPointerFactory
{
    /// <summary>
    /// Create a smart pointer with automatic resource ownership.
    /// </summary>
    public static SmartPointer<T> Create<T>(T? value) where T : class
        => new(value, ownsResource: true);

    /// <summary>
    /// Create a smart pointer that doesn't own the resource.
    /// </summary>
    public static SmartPointer<T> CreateUnowned<T>(T? value) where T : class
        => new(value, ownsResource: false);
}
