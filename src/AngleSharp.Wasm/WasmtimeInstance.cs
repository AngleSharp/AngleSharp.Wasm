namespace AngleSharp.Wasm;

using System;
using System.Threading;
using System.Threading.Tasks;
using Wasmtime;

/// <summary>
/// Wraps a Wasmtime instance and store.
/// </summary>
public sealed class WasmtimeInstance : IWasmInstance
{
    private readonly Store _store;
    private readonly Instance _instance;
    private bool _disposed;

    internal WasmtimeInstance(Store store, Instance instance)
    {
        _store = store;
        _instance = instance;
    }

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(string exportName, object?[]? arguments = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(exportName))
        {
            throw new ArgumentException("Export name must be provided.", nameof(exportName));
        }

        var function = _instance.GetFunction(exportName);

        if (function is null)
        {
            throw new InvalidOperationException($"The export '{exportName}' is not a function.");
        }

        if (arguments is { Length: > 0 })
        {
            throw new NotSupportedException("Passing function arguments is not implemented yet in this scaffold.");
        }

        var result = function.Invoke();
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _store.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmtimeInstance));
        }
    }
}
