namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using System;

/// <summary>
/// JavaScript-visible WebAssembly table.
/// </summary>
[DomName("Table")]
public sealed class WasmJsTable
{
    private readonly IWasmTable _table;

    internal WasmJsTable(IWasmTable table)
    {
        _table = table;
    }

    /// <summary>
    /// Gets the current number of elements.
    /// </summary>
    [DomName("length")]
    [DomAccessor(Accessors.Getter)]
    public ulong Length => _table.Length;

    /// <summary>
    /// Gets an element by index.
    /// </summary>
    /// <param name="index">The element index.</param>
    /// <returns>The table element.</returns>
    [DomName("get")]
    public object? Get(uint index)
    {
        EnsureIndex(index);
        return Wrap(_table.Get(index));
    }

    /// <summary>
    /// Sets an element by index.
    /// </summary>
    /// <param name="index">The element index.</param>
    /// <param name="value">The new element value.</param>
    [DomName("set")]
    public void Set(uint index, object? value)
    {
        EnsureIndex(index);
        _table.Set(index, Unwrap(value));
    }

    /// <summary>
    /// Grows the table and initializes the new elements.
    /// </summary>
    /// <param name="delta">The number of elements to add.</param>
    /// <param name="value">The initial value for new elements.</param>
    /// <returns>The previous table length.</returns>
    [DomName("grow")]
    public ulong Grow(uint delta, object? value = null) => _table.Grow(delta, Unwrap(value));

    private void EnsureIndex(uint index)
    {
        if (index >= _table.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private static object? Wrap(object? value) =>
        value is IWasmFunction function ? new WasmJsExportedFunction(function) : value;

    private static object? Unwrap(object? value) =>
        value is WasmJsExportedFunction { Function: not null } function ? function.Function : value;
}