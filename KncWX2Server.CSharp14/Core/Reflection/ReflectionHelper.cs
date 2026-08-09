using System.Reflection;

namespace KncWX2Server.Core.Reflection;

/// <summary>
/// Helper for runtime type information (RTTI) replacement.
/// Provides type metadata and reflection utilities for the server framework.
/// </summary>
public static class ReflectionHelper
{
    private static readonly Dictionary<Type, TypeMetadata> _metadataCache = new();

    /// <summary>
    /// Gets type metadata with caching.
    /// </summary>
    public static TypeMetadata GetMetadata<T>() where T : class
        => GetMetadata(typeof(T));

    /// <summary>
    /// Gets type metadata for a specific type.
    /// </summary>
    public static TypeMetadata GetMetadata(Type type)
    {
        lock (_metadataCache)
        {
            if (_metadataCache.TryGetValue(type, out var metadata))
            {
                return metadata;
            }

            metadata = new TypeMetadata(type);
            _metadataCache[type] = metadata;
            return metadata;
        }
    }

    /// <summary>
    /// Checks if a type inherits from a base type.
    /// </summary>
    public static bool IsAssignableTo<TBase>(Type type) where TBase : class
        => typeof(TBase).IsAssignableFrom(type);

    /// <summary>
    /// Gets all methods with a specific attribute.
    /// </summary>
    public static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(Type type)
        where TAttribute : Attribute
        => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<TAttribute>() != null);
}

/// <summary>
/// Represents type metadata for a class.
/// </summary>
public sealed class TypeMetadata
{
    private readonly Type _type;
    private readonly PropertyInfo[] _properties;
    private readonly MethodInfo[] _methods;
    private readonly FieldInfo[] _fields;

    public string TypeName => _type.Name;
    public string FullName => _type.FullName ?? string.Empty;
    public Type BaseType => _type.BaseType ?? typeof(object);

    public TypeMetadata(Type type)
    {
        _type = type;
        _properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        _methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        _fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
    }

    public PropertyInfo[] GetProperties() => _properties;
    public MethodInfo[] GetMethods() => _methods;
    public FieldInfo[] GetFields() => _fields;

    public bool HasMethod(string methodName)
        => _methods.Any(m => m.Name == methodName);

    public MethodInfo? FindMethod(string methodName, params Type[] parameterTypes)
        => _type.GetMethod(methodName, parameterTypes);

    public bool IsAbstract => _type.IsAbstract;
    public bool IsInterface => _type.IsInterface;
    public bool IsGeneric => _type.IsGenericType;
}
