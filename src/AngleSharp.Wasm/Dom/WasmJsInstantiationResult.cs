namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;

/// <summary>
/// Contains a compiled module and its instantiated instance.
/// </summary>
[DomName("WebAssemblyInstantiatedSource")]
public sealed class WasmJsInstantiationResult
{
    internal WasmJsInstantiationResult(WasmJsModule module, WasmJsInstance instance)
    {
        Module = module;
        Instance = instance;
    }

    /// <summary>
    /// Gets the compiled module.
    /// </summary>
    [DomName("module")]
    [DomAccessor(Accessors.Getter)]
    public WasmJsModule Module { get; }

    /// <summary>
    /// Gets the instantiated module instance.
    /// </summary>
    [DomName("instance")]
    [DomAccessor(Accessors.Getter)]
    public WasmJsInstance Instance { get; }
}