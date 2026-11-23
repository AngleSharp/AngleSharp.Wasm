namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;

/// <summary>
/// An instance of a module (WebAssembly.Instance).
/// </summary>
[DomName("Instance")]
[DomExposed("WebAssembly")]
public sealed class WasmJsInstance
{
    private readonly WasmJsExports _exports;
    private readonly IDictionary<string, object?> _scriptExports;

    internal WasmJsInstance(IWasmInstance instance, IReadOnlyList<WasmModuleExportDescriptor> exportsMetadata)
    {
        Instance = instance;
        _exports = new WasmJsExports(this, exportsMetadata);
        _scriptExports = new Dictionary<string, object?>();
    }

    internal WasmJsInstance(WasmJsRealm realm, WasmtimeInstance instance, WasmJsModule module)
    {
        Instance = instance;
        _exports = new WasmJsExports(this, module.ExportsMetadata);
        _scriptExports = realm.GetExports(instance.Native, ((WasmtimeCompiledModule)module.Module).Module);
    }

    /// <summary>
    /// Instantiates a module (new WebAssembly.Instance), synchronously.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="module">The module.</param>
    /// <param name="importObject">What the module imports, by module and name.</param>
    [DomConstructor]
    public WasmJsInstance(IWindow window, WasmJsModule module, IDictionary<string, IDictionary<string, WasmJsImportValue>>? importObject = null)
        : this(WebAssembly.InstantiateCore(window, module, importObject))
    {
    }

    private WasmJsInstance(WasmJsInstance instance)
    {
        Instance = instance.Instance;
        _exports = new WasmJsExports(this, instance._exports.Metadata);
        _scriptExports = instance._scriptExports;
    }

    internal IWasmInstance Instance { get; }

    /// <summary>
    /// Gets the exports of the instance for C#, by name.
    /// </summary>
    public WasmJsExports Exports => _exports;

    /// <summary>
    /// Gets the exports as scripts see them (instance.exports): an object with the functions,
    /// memories, tables and globals the module exports.
    /// </summary>
    [DomName("exports")]
    [DomAccessor(Accessors.Getter)]
    public IDictionary<string, object?> ScriptExports => _scriptExports;

    /// <summary>
    /// Invokes an exported function.
    /// </summary>
    /// <param name="exportName">The exported function name.</param>
    /// <param name="arguments">The invocation arguments.</param>
    /// <returns>The invocation result.</returns>
    public object? Invoke(string exportName, params object?[] arguments)
    {
        try
        {
            return Instance.InvokeAsync(exportName, arguments).AsTask().GetAwaiter().GetResult();
        }
        catch (Exception exception) when (WebAssembly.ShouldTranslateOperationFailure(exception))
        {
            throw new WasmJsRuntimeError(exception.Message, exception);
        }
    }
}

