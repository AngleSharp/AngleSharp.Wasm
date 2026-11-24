namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// JavaScript-visible module handle.
/// </summary>
[DomName("Module")]
[DomExposed("WebAssembly")]
public sealed class WasmJsModule
{
    private readonly WasmModuleMetadata _metadata;

    internal WasmJsModule(IWasmRuntime runtime, IWasmCompiledModule module, byte[] moduleBytes)
    {
        Runtime = runtime;
        Module = module;
        _metadata = WasmModuleMetadata.Parse(moduleBytes);
    }

    /// <summary>
    /// Compiles a module (new WebAssembly.Module), synchronously.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="bytes">The bytes of the module.</param>
    [DomConstructor]
    public WasmJsModule(IWindow window, byte[] bytes)
        : this(WebAssembly.Compile(window, bytes))
    {
    }

    private WasmJsModule(WasmJsModule compiled)
    {
        Runtime = compiled.Runtime;
        Module = compiled.Module;
        _metadata = compiled._metadata;
    }

    /// <summary>
    /// Gets the exports of a module (WebAssembly.Module.exports): their names and kinds.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>The descriptions of the exports.</returns>
    [DomName("exports")]
    public static WasmModuleExportDescriptor[] Exports(WasmJsModule module) =>
        (module ?? throw new ArgumentNullException(nameof(module))).Exports();

    /// <summary>
    /// Gets the imports of a module (WebAssembly.Module.imports): their modules, names and kinds.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>The descriptions of the imports.</returns>
    [DomName("imports")]
    public static WasmModuleImportDescriptor[] Imports(WasmJsModule module) =>
        (module ?? throw new ArgumentNullException(nameof(module))).Imports();

    /// <summary>
    /// Gets the custom sections of a module with the given name (WebAssembly.Module.customSections).
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="sectionName">The name of the sections.</param>
    /// <returns>The contents of the sections, ArrayBuffers for scripts.</returns>
    [DomName("customSections")]
    public static byte[][] CustomSections(WasmJsModule module, string sectionName) =>
        (module ?? throw new ArgumentNullException(nameof(module))).CustomSections(sectionName);

    internal IWasmRuntime Runtime { get; }

    internal IWasmCompiledModule Module { get; }

    internal IReadOnlyList<WasmModuleExportDescriptor> ExportsMetadata => _metadata.Exports;

    /// <summary>
    /// Gets this module's exported names and kinds.
    /// </summary>
    /// <returns>The export descriptors.</returns>
    public WasmModuleExportDescriptor[] Exports() => _metadata.Exports.ToArray();

    /// <summary>
    /// Gets this module's imported names and kinds.
    /// </summary>
    /// <returns>The import descriptors.</returns>
    public WasmModuleImportDescriptor[] Imports() => _metadata.Imports.ToArray();

    /// <summary>
    /// Gets the custom sections with the specified name.
    /// </summary>
    /// <param name="sectionName">The custom section name.</param>
    /// <returns>The custom section payloads.</returns>
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
