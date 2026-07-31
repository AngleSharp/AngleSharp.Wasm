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
public sealed class WasmtimeWasmRuntime : IWasmRuntime
{
    private readonly Engine _engine;
    private bool _disposed;

    /// <summary>
    /// Creates a new runtime.
    /// </summary>
    public WasmtimeWasmRuntime()
    {
        _engine = new Engine();
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

        if (imports is not null)
        {
            foreach (var import in imports)
            {
                BindImport(linker, import);
            }
        }

        var instance = linker.Instantiate(store, wasmtimeModule.Module);

        return ValueTask.FromResult<IWasmInstance>(new WasmtimeInstance(store, instance));
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
                WasmValueType.Int32 => arguments[i].AsInt32(),
                WasmValueType.Int64 => arguments[i].AsInt64(),
                WasmValueType.Float32 => arguments[i].AsSingle(),
                WasmValueType.Float64 => arguments[i].AsDouble(),
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
