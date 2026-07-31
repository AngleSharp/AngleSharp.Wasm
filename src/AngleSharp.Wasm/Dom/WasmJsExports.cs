namespace AngleSharp.Wasm.Dom;

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
                m => m.Kind == "function"
                    ? (object)new WasmJsExportedFunction(owner, m.Name)
                    : new WasmJsExportValue(m.Name, m.Kind));
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
