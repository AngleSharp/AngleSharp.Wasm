namespace AngleSharp.Wasm.Dom;

using System;
using AngleSharp.Attributes;
using AngleSharp.Dom;
using Wasmtime;

/// <summary>
/// A table of WebAssembly (WebAssembly.Table), e.g. of functions.
/// </summary>
[DomName("Table")]
[DomExposed("WebAssembly")]
public sealed class WasmJsTable
{
    private readonly IWasmTable _table;
    private readonly WasmJsRealm? _realm;

    internal WasmJsTable(IWasmTable table)
    {
        _table = table;
    }

    internal WasmJsTable(WasmJsRealm realm, WasmtimeTable table)
    {
        _table = table;
        _realm = realm;
    }

    /// <summary>
    /// Creates a table (new WebAssembly.Table).
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="descriptor">The type of the elements and the initial and maximum length.</param>
    /// <param name="value">The value of the elements, if any; else null.</param>
    [DomConstructor]
    public WasmJsTable(IWindow window, WasmJsTableDescriptor descriptor, object? value = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _realm = WebAssembly.GetRealm(window);
        var kind = WasmJsGlobal.ParseKind(descriptor.Element);

        if (kind != ValueKind.FuncRef && kind != ValueKind.ExternRef)
        {
            throw new ArgumentException($"'{descriptor.Element}' is no type of the elements of a table.");
        }

        var initial = ToLength(descriptor.Initial);
        var maximum = descriptor.Maximum is double max ? ToLength(max) : uint.MaxValue;
        var element = value is null || value == System.Reflection.Missing.Value ? WasmJsGlobal.DefaultOf(kind) : _realm.ToWasm(value, kind);
        _table = new WasmtimeTable(_realm.Context, new Table(_realm.Store, kind == ValueKind.FuncRef ? TableKind.FuncRef : TableKind.ExternRef, element, initial, maximum));
    }

    internal Table? Native => (_table as WasmtimeTable)?.Native;

    /// <summary>
    /// Gets the number of elements.
    /// </summary>
    [DomName("length")]
    [DomAccessor(Accessors.Getter)]
    public ulong Length => _table.Length;

    /// <summary>
    /// Gets an element.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The element, e.g. a function or null.</returns>
    [DomName("get")]
    public object? Get(uint index)
    {
        EnsureIndex(index);

        if (_realm is not null && Native is Table table)
        {
            return _realm.FromWasm(table.GetElement(index));
        }

        return Wrap(_table.Get(index));
    }

    /// <summary>
    /// Sets an element.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="value">The element, e.g. an exported function or null.</param>
    [DomName("set")]
    public void Set(uint index, object? value = null)
    {
        EnsureIndex(index);

        if (_realm is not null && Native is Table table)
        {
            table.SetElement(index, ToElement(table, value));
        }
        else
        {
            _table.Set(index, Unwrap(value));
        }
    }

    /// <summary>
    /// Grows the table.
    /// </summary>
    /// <param name="delta">The number of elements to add.</param>
    /// <param name="value">The value of the new elements, if any; else null.</param>
    /// <returns>The length before.</returns>
    [DomName("grow")]
    public ulong Grow(uint delta, object? value = null) =>
        _realm is not null && Native is Table table
            ? table.Grow(delta, ToElement(table, value))
            : _table.Grow(delta, Unwrap(value));

    private object? ToElement(Table table, object? value)
    {
        var kind = table.Kind == TableKind.FuncRef ? ValueKind.FuncRef : ValueKind.ExternRef;
        return value is null || value == System.Reflection.Missing.Value ? WasmJsGlobal.DefaultOf(kind) : _realm!.ToWasm(value, kind);
    }

    private static uint ToLength(double length) =>
        length >= 0 && length <= uint.MaxValue && length == Math.Floor(length)
            ? (uint)length
            : throw new ArgumentOutOfRangeException(nameof(length));

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
