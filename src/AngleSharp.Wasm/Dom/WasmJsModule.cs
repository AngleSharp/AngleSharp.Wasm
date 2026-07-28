namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// JavaScript-visible module handle.
/// </summary>
[DomName("Module")]
public sealed class WasmJsModule
{
    private readonly WasmModuleMetadata _metadata;

    internal WasmJsModule(IWasmRuntime runtime, IWasmCompiledModule module, byte[] moduleBytes)
    {
        Runtime = runtime;
        Module = module;
        _metadata = WasmModuleMetadata.Parse(moduleBytes);
    }

    internal IWasmRuntime Runtime { get; }

    internal IWasmCompiledModule Module { get; }

    internal IReadOnlyList<WasmModuleExportDescriptor> ExportsMetadata => _metadata.Exports;

    /// <summary>
    /// Gets this module's exported names and kinds.
    /// </summary>
    /// <returns>The export descriptors.</returns>
    [DomName("exports")]
    public WasmModuleExportDescriptor[] Exports() => _metadata.Exports.ToArray();

    /// <summary>
    /// Gets this module's imported names and kinds.
    /// </summary>
    /// <returns>The import descriptors.</returns>
    [DomName("imports")]
    public WasmModuleImportDescriptor[] Imports() => _metadata.Imports.ToArray();

    /// <summary>
    /// Gets the custom sections with the specified name.
    /// </summary>
    /// <param name="sectionName">The custom section name.</param>
    /// <returns>The custom section payloads.</returns>
    [DomName("customSections")]
    public byte[][] CustomSections(string sectionName)
    {
        if (sectionName is null)
        {
            throw new ArgumentNullException(nameof(sectionName));
        }

        return _metadata.CustomSections
            .Where(m => String.Equals(m.Name, sectionName, StringComparison.Ordinal))
            .Select(m => m.Payload)
            .ToArray();
    }
}
