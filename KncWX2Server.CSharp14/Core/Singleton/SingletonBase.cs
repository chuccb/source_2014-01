namespace KncWX2Server.Core.Singleton;

/// <summary>
/// Base class for implementing singleton pattern in C# 14.
/// Thread-safe singleton with lazy initialization.
/// </summary>
/// <typeparam name="T">The singleton type</typeparam>
public abstract class SingletonBase<T> where T : SingletonBase<T>
{
    private static readonly Lazy<T> _instance = new(() => CreateInstance());

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static T Instance => _instance.Value;

    /// <summary>
    /// Creates a new instance of the singleton.
    /// Override this method to customize initialization.
    /// </summary>
    protected static T CreateInstance()
    {
        var constructor = typeof(T).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Singleton {typeof(T).Name} must have a protected or private parameterless constructor.");
        }

        return (T)constructor.Invoke(null)!;
    }

    /// <summary>
    /// Called when the singleton is initialized.
    /// Override to perform initialization logic.
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Called when the singleton is disposed.
    /// Override to perform cleanup logic.
    /// </summary>
    protected virtual void OnDispose() { }
}

/// <summary>
/// Thread-safe singleton provider using a factory method.
/// </summary>
public class Singleton<T> where T : class
{
    private readonly Lazy<T> _instance;

    public Singleton(Func<T> factory)
    {
        _instance = new Lazy<T>(factory);
    }

    public T Instance => _instance.Value;
}
