namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;

/// <summary>
/// JavaScript-visible module handle.
/// </summary>
[DomName("Module")]
public sealed class WasmJsModule
{
    internal WasmJsModule(IWasmRuntime runtime, IWasmCompiledModule module)
    {
        Runtime = runtime;
        Module = module;
    }

    internal IWasmRuntime Runtime { get; }

    internal IWasmCompiledModule Module { get; }
}
