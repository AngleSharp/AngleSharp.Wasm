namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using System.Collections.Generic;

/// <summary>
/// JavaScript-visible instance handle.
/// </summary>
[DomName("Instance")]
public sealed class WasmJsInstance
{
    private readonly WasmJsExports _exports;

    internal WasmJsInstance(IWasmInstance instance, IReadOnlyList<WasmModuleExportDescriptor> exportsMetadata)
    {
        Instance = instance;
        _exports = new WasmJsExports(this, exportsMetadata);
    }

    internal IWasmInstance Instance { get; }

    /// <summary>
    /// Gets the module exports object.
    /// </summary>
    [DomName("exports")]
    [DomAccessor(Accessors.Getter)]
    public WasmJsExports Exports => _exports;

    /// <summary>
    /// Invokes an exported function.
    /// </summary>
    /// <param name="exportName">The exported function name.</param>
    /// <param name="arguments">The invocation arguments.</param>
    /// <returns>The invocation result.</returns>
    [DomName("invoke")]
    public object? Invoke(string exportName, params object?[] arguments) =>
        Instance.InvokeAsync(exportName, arguments).AsTask().GetAwaiter().GetResult();
}

