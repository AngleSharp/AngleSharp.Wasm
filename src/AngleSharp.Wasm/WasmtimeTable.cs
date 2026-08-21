namespace AngleSharp.Wasm;

using System;
using Wasmtime;

internal sealed class WasmtimeTable : IWasmTable
{
    private readonly Table _table;

    internal WasmtimeTable(Table table)
    {
        _table = table;
    }

    public ulong Length => _table.GetSize();

    public object? Get(uint index) => Wrap(_table.GetElement(index));

    public void Set(uint index, object? value) => _table.SetElement(index, Unwrap(value));

    public ulong Grow(uint delta, object? value) => _table.Grow(delta, Unwrap(value));

    private static object? Wrap(object? value) => value switch
    {
        Function { IsNull: true } => null,
        Function function => new WasmtimeFunction(function),
        _ => value,
    };

    private object? Unwrap(object? value) => value switch
    {
        null when _table.Kind == TableKind.FuncRef => Function.Null,
        null => null,
        WasmtimeFunction function => function.Function,
        IWasmFunction => throw new ArgumentException("The function reference belongs to a different WebAssembly runtime.", nameof(value)),
        _ => value,
    };
}