namespace AngleSharp.Wasm.Dom;

using System.Numerics;
using AngleSharp.Attributes;

/// <summary>
/// A function as scripts see one: an exported function of WebAssembly, which scripts call, or a
/// function of a script, which a module imports. It takes all arguments given (a missing one,
/// i.e. undefined, is <see cref="System.Reflection.Missing.Value"/>), and gives the result.
/// </summary>
/// <param name="arguments">The arguments.</param>
/// <returns>The result; <see cref="System.Reflection.Missing.Value"/> for undefined.</returns>
public delegate object? WasmJsCallable(object?[] arguments);

/// <summary>
/// What an import object may hold for an import: a function, a number or BigInt (for an
/// immutable global), or a WebAssembly.Memory, Table or Global.
/// </summary>
public sealed class WasmJsImportValue
{
    private WasmJsImportValue(object? value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the value given.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the function given, if a function was given.
    /// </summary>
    public WasmJsCallable? Function => Value as WasmJsCallable;

    /// <summary>
    /// Takes a function.
    /// </summary>
    public static implicit operator WasmJsImportValue(WasmJsCallable value) => new(value);

    /// <summary>
    /// Takes a number.
    /// </summary>
    public static implicit operator WasmJsImportValue(double value) => new(value);

    /// <summary>
    /// Takes a BigInt.
    /// </summary>
    public static implicit operator WasmJsImportValue(BigInteger value) => new(value);

    /// <summary>
    /// Takes a memory.
    /// </summary>
    public static implicit operator WasmJsImportValue(WasmJsMemory value) => new(value);

    /// <summary>
    /// Takes a table.
    /// </summary>
    public static implicit operator WasmJsImportValue(WasmJsTable value) => new(value);

    /// <summary>
    /// Takes a global.
    /// </summary>
    public static implicit operator WasmJsImportValue(WasmJsGlobal value) => new(value);
}

/// <summary>
/// The descriptor of a new WebAssembly.Memory.
/// </summary>
[DomName("MemoryDescriptor")]
[DomNoInterfaceObject]
public sealed class WasmJsMemoryDescriptor
{
    /// <summary>
    /// Gets or sets the initial size in pages (64 KiB each).
    /// </summary>
    [DomName("initial")]
    public double Initial { get; set; }

    /// <summary>
    /// Gets or sets the maximum size in pages, if any.
    /// </summary>
    [DomName("maximum")]
    public double? Maximum { get; set; }

    /// <summary>
    /// Gets or sets if the memory is shared (not supported).
    /// </summary>
    [DomName("shared")]
    public bool Shared { get; set; }
}

/// <summary>
/// The descriptor of a new WebAssembly.Table.
/// </summary>
[DomName("TableDescriptor")]
[DomNoInterfaceObject]
public sealed class WasmJsTableDescriptor
{
    /// <summary>
    /// Gets or sets the type of the elements: "anyfunc" (or "funcref"), or "externref".
    /// </summary>
    [DomName("element")]
    public string? Element { get; set; }

    /// <summary>
    /// Gets or sets the initial number of elements.
    /// </summary>
    [DomName("initial")]
    public double Initial { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of elements, if any.
    /// </summary>
    [DomName("maximum")]
    public double? Maximum { get; set; }
}

/// <summary>
/// The descriptor of a new WebAssembly.Global.
/// </summary>
[DomName("GlobalDescriptor")]
[DomNoInterfaceObject]
public sealed class WasmJsGlobalDescriptor
{
    /// <summary>
    /// Gets or sets the type: "i32", "i64", "f32", "f64", "anyfunc" (or "funcref"), or "externref".
    /// </summary>
    [DomName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets if the global may be changed.
    /// </summary>
    [DomName("mutable")]
    public bool Mutable { get; set; }
}
