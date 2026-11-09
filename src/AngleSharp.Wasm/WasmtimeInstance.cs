namespace AngleSharp.Wasm;

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Wasmtime;

/// <summary>
/// Wraps a Wasmtime instance and store.
/// </summary>
public sealed class WasmtimeInstance : IWasmInstance, IWasmFunctionProvider, IWasmGlobalProvider, IWasmMemoryProvider, IWasmTableProvider
{
    private readonly WasmtimeStoreContext _context;
    private readonly Instance _instance;
    private bool _disposed;

    internal WasmtimeInstance(WasmtimeStoreContext context, Instance instance)
    {
        _context = context;
        _instance = instance;
    }

    /// <inheritdoc />
    public IWasmFunction? GetFunction(string exportName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(exportName))
        {
            throw new ArgumentException("Export name must be provided.", nameof(exportName));
        }

        var function = _instance.GetFunction(exportName);
        return function is null ? null : new WasmtimeFunction(_context, function);
    }

    /// <inheritdoc />
    public IWasmGlobal? GetGlobal(string exportName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(exportName))
        {
            throw new ArgumentException("Export name must be provided.", nameof(exportName));
        }

        var global = _instance.GetGlobal(exportName);
        return global is null ? null : new WasmtimeGlobal(_context, global);
    }

    /// <inheritdoc />
    public IWasmMemory? GetMemory(string exportName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(exportName))
        {
            throw new ArgumentException("Export name must be provided.", nameof(exportName));
        }

        var memory = _instance.GetMemory(exportName);
        return memory is null ? null : new WasmtimeMemory(memory);
    }

    /// <inheritdoc />
    public IWasmTable? GetTable(string exportName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(exportName))
        {
            throw new ArgumentException("Export name must be provided.", nameof(exportName));
        }

        var table = _instance.GetTable(exportName);
        return table is null ? null : new WasmtimeTable(_context, table);
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

        var parameters = function.Parameters;
        var invocationArguments = arguments ?? Array.Empty<object?>();

        if (invocationArguments.Length != parameters.Count)
        {
            throw new ArgumentException($"Export '{exportName}' expects {parameters.Count} argument(s), but {invocationArguments.Length} were provided.", nameof(arguments));
        }

        if (invocationArguments.Length == 0)
        {
            return ValueTask.FromResult(_context.Run(static function => function.Invoke(), function));
        }

        var boxedArguments = new ValueBox[invocationArguments.Length];

        for (var i = 0; i < boxedArguments.Length; i++)
        {
            boxedArguments[i] = parameters[i] switch
            {
                ValueKind.Int32 => (ValueBox)Convert.ToInt32(invocationArguments[i], CultureInfo.InvariantCulture),
                ValueKind.Int64 => (ValueBox)Convert.ToInt64(invocationArguments[i], CultureInfo.InvariantCulture),
                ValueKind.Float32 => (ValueBox)Convert.ToSingle(invocationArguments[i], CultureInfo.InvariantCulture),
                ValueKind.Float64 => (ValueBox)Convert.ToDouble(invocationArguments[i], CultureInfo.InvariantCulture),
                _ => throw new NotSupportedException($"Unsupported parameter type '{parameters[i]}' on export '{exportName}'."),
            };
        }

        var result = _context.Run(static state => state.Function.Invoke(state.Arguments), (Function: function, Arguments: boxedArguments));
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
        _context.Store.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmtimeInstance));
        }
    }
}
