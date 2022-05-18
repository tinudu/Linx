using System;

namespace Linx.AsyncEnumerable;
/// <summary>
/// Deferred access to a value of type <typeparamref name="T"/>.
/// </summary>
public struct Deferred<T>
{
    internal interface IProvider
    {
        T GetResult(short version);
    }

    private readonly IProvider? _provider;
    private readonly short _version;

    internal Deferred(IProvider provider, short version)
    {
        _provider = provider;
        _version = version;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <see cref="Deferred{T}"/> has expired.</exception>
    public T GetResult() => _provider is null ? default! : _provider.GetResult(_version);
}

