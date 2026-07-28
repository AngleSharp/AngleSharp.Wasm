namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;

/// <summary>
/// JavaScript-visible exported function wrapper.
/// </summary>
[DomName("ExportedFunction")]
public sealed class WasmJsExportedFunction
{
    private readonly WasmJsInstance _owner;
    private readonly string _name;

    internal WasmJsExportedFunction(WasmJsInstance owner, string name)
    {
        _owner = owner;
        _name = name;
    }

    /// <summary>
    /// Invokes the export function.
    /// </summary>
    /// <param name="arguments">The function arguments.</param>
    /// <returns>The invocation result.</returns>
    [DomName("invoke")]
    public object? Invoke(params object?[] arguments) => _owner.Invoke(_name, arguments);
}
