namespace AngleSharp.Wasm;

using System;
using System.Globalization;
using Wasmtime;

internal sealed class WasmtimeGlobal : IWasmGlobal
{
    private readonly Global _global;

    internal WasmtimeGlobal(Global global)
    {
        _global = global;
    }

    public bool IsMutable => _global.Mutability == Mutability.Mutable;

    public object? GetValue() => Wrap(_global.GetValue());

    public void SetValue(object? value)
    {
        if (!IsMutable)
        {
            throw new InvalidOperationException("The WebAssembly global is immutable.");
        }

        _global.SetValue(Unwrap(value));
    }

    private static object? Wrap(object? value) => value switch
    {
        Function { IsNull: true } => null,
        Function function => new WasmtimeFunction(function),
        _ => value,
    };

    private object? Unwrap(object? value) => _global.Kind switch
    {
        ValueKind.Int32 => Convert.ToInt32(value, CultureInfo.InvariantCulture),
        ValueKind.Int64 => Convert.ToInt64(value, CultureInfo.InvariantCulture),
        ValueKind.Float32 => Convert.ToSingle(value, CultureInfo.InvariantCulture),
        ValueKind.Float64 => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        ValueKind.FuncRef => UnwrapFunction(value),
        _ => value,
    };

    private static object UnwrapFunction(object? value) => value switch
    {
        null => Function.Null,
        WasmtimeFunction function => function.Function,
        IWasmFunction => throw new ArgumentException("The function reference belongs to a different WebAssembly runtime.", nameof(value)),
        _ => value,
    };
}