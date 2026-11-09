namespace AngleSharp.Wasm;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasmtime;

/// <summary>
/// Wasmtime-backed <see cref="IWasmRuntime"/> implementation.
/// </summary>
public sealed class WasmtimeWasmRuntime : IWasmRuntime, IWasmValidator
{
    private readonly Engine _engine;
    private readonly long? _maximumMemorySize;
    private readonly uint? _maximumTableElements;
    private readonly ulong? _fuel;
    private bool _disposed;

    /// <summary>
    /// Creates a new runtime without resource limits.
    /// </summary>
    public WasmtimeWasmRuntime()
        : this(new WasmtimeWasmRuntimeOptions())
    {
    }

    /// <summary>
    /// Creates a new runtime with the given resource limits.
    /// </summary>
    /// <param name="options">The resource limits. The values are read once, here.</param>
    public WasmtimeWasmRuntime(WasmtimeWasmRuntimeOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.MaximumMemorySize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaximumMemorySize must not be negative.");
        }

        _maximumMemorySize = options.MaximumMemorySize;
        _maximumTableElements = options.MaximumTableElements;
        _fuel = options.Fuel;
        _engine = _fuel is null ? new Engine() : new Engine(new Config().WithFuelConsumption(true));
    }

    /// <inheritdoc />
    public bool Validate(ReadOnlyMemory<byte> moduleBytes)
    {
        ThrowIfDisposed();
        return Module.Validate(_engine, moduleBytes.Span) is null;
    }

    /// <inheritdoc />
    public ValueTask<IWasmCompiledModule> CompileAsync(ReadOnlyMemory<byte> moduleBytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var module = Module.FromBytes(_engine, "anglesharp-wasm", moduleBytes.ToArray());
        return ValueTask.FromResult<IWasmCompiledModule>(new WasmtimeCompiledModule(_engine, module));
    }

    /// <inheritdoc />
    public ValueTask<IWasmInstance> InstantiateAsync(
        IWasmCompiledModule compiledModule,
        IEnumerable<WasmImportFunction>? imports = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (compiledModule is not WasmtimeCompiledModule wasmtimeModule)
        {
            throw new ArgumentException("Compiled module must be created by WasmtimeWasmRuntime.", nameof(compiledModule));
        }

        var linker = new Linker(wasmtimeModule.Engine);
        var store = new Store(wasmtimeModule.Engine);

        try
        {
            if (_maximumMemorySize is not null || _maximumTableElements is not null)
            {
                store.SetLimits(_maximumMemorySize, _maximumTableElements, null, null, null);
            }

            if (imports is not null)
            {
                foreach (var import in imports)
                {
                    BindImport(linker, import);
                }
            }

            var context = new WasmtimeStoreContext(store, _fuel);
            var instance = context.Run(() => linker.Instantiate(store, wasmtimeModule.Module));

            return ValueTask.FromResult<IWasmInstance>(new WasmtimeInstance(context, instance));
        }
        catch
        {
            store.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmtimeWasmRuntime));
        }
    }

    private static void BindImport(Linker linker, WasmImportFunction import)
    {
        var parameterKinds = import.ParameterTypes.Select(Map).ToArray();
        var resultKinds = import.ResultTypes.Select(Map).ToArray();

        linker.DefineFunction(
            import.ModuleName,
            import.FunctionName,
            (Caller _, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> results) =>
            {
                var callbackArguments = ConvertArguments(arguments, import.ParameterTypes);
                var callbackResult = import.Callback(callbackArguments);
                FillResults(results, import.ResultTypes, callbackResult);
            },
            parameterKinds,
            resultKinds);
    }

    private static ValueKind Map(WasmValueType type) => type switch
    {
        WasmValueType.Int32 => ValueKind.Int32,
        WasmValueType.Int64 => ValueKind.Int64,
        WasmValueType.Float32 => ValueKind.Float32,
        WasmValueType.Float64 => ValueKind.Float64,
        _ => throw new NotSupportedException($"Unsupported WasmValueType '{type}'."),
    };

    private static object?[] ConvertArguments(ReadOnlySpan<ValueBox> arguments, IReadOnlyList<WasmValueType> types)
    {
        var converted = new object?[arguments.Length];

        for (var i = 0; i < arguments.Length; i++)
        {
            converted[i] = types[i] switch
            {
                WasmValueType.Int32 => (object)arguments[i].AsInt32(),
                WasmValueType.Int64 => (object)arguments[i].AsInt64(),
                WasmValueType.Float32 => (object)arguments[i].AsSingle(),
                WasmValueType.Float64 => (object)arguments[i].AsDouble(),
                _ => throw new NotSupportedException($"Unsupported argument type '{types[i]}'."),
            };
        }

        return converted;
    }

    private static void FillResults(Span<ValueBox> results, IReadOnlyList<WasmValueType> types, object? callbackResult)
    {
        if (results.Length == 0)
        {
            return;
        }

        if (results.Length == 1)
        {
            results[0] = ToValueBox(callbackResult, types[0]);
            return;
        }

        if (callbackResult is not object?[] values || values.Length != results.Length)
        {
            throw new InvalidOperationException("Import callback must return object[] with the same length as result types for multi-value results.");
        }

        for (var i = 0; i < results.Length; i++)
        {
            results[i] = ToValueBox(values[i], types[i]);
        }
    }

    private static ValueBox ToValueBox(object? value, WasmValueType type) => type switch
    {
        WasmValueType.Int32 => (ValueBox)Convert.ToInt32(value, CultureInfo.InvariantCulture),
        WasmValueType.Int64 => (ValueBox)Convert.ToInt64(value, CultureInfo.InvariantCulture),
        WasmValueType.Float32 => (ValueBox)Convert.ToSingle(value, CultureInfo.InvariantCulture),
        WasmValueType.Float64 => (ValueBox)Convert.ToDouble(value, CultureInfo.InvariantCulture),
        _ => throw new NotSupportedException($"Unsupported result type '{type}'."),
    };
}
