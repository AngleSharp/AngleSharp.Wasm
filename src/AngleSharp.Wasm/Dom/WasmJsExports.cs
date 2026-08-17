namespace AngleSharp.Wasm.Dom;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Attributes;

/// <summary>
/// JavaScript-visible exports object.
/// </summary>
[DomName("Exports")]
public sealed class WasmJsExports
{
    private readonly IReadOnlyDictionary<string, object> _entries;

    internal WasmJsExports(WasmJsInstance owner, IReadOnlyList<WasmModuleExportDescriptor> exportsMetadata)
    {
        _entries = exportsMetadata
            .ToDictionary(
                m => m.Name,
                m => CreateExport(owner, m));
    }

    private static object CreateExport(WasmJsInstance owner, WasmModuleExportDescriptor metadata) =>
        metadata.Kind switch
        {
            "function" => new WasmJsExportedFunction(owner, metadata.Name),
            "memory" => CreateMemory(owner, metadata.Name),
            _ => new WasmJsExportValue(metadata.Name, metadata.Kind),
        };

    private static WasmJsMemory CreateMemory(WasmJsInstance owner, string name)
    {
        if (owner.Instance is not IWasmMemoryProvider provider)
        {
            throw new NotSupportedException("The configured WebAssembly runtime does not support memory exports.");
        }

        var memory = provider.GetMemory(name)
            ?? throw new InvalidOperationException($"The export '{name}' is not a memory.");
        return new WasmJsMemory(memory);
    }

    /// <summary>
    /// Gets the export entry by name.
    /// </summary>
    /// <param name="name">The export name.</param>
    /// <returns>The export entry, if present.</returns>
    [DomName("item")]
    [DomAccessor(Accessors.Getter)]
    public object? this[string name] => _entries.TryGetValue(name, out var value) ? value : null;

    /// <summary>
    /// Gets the available export names.
    /// </summary>
    /// <returns>The export names.</returns>
    [DomName("keys")]
    public string[] Keys() => _entries.Keys.ToArray();
}
