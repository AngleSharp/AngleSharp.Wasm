namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;

/// <summary>
/// JavaScript-visible WebAssembly global.
/// </summary>
[DomName("Global")]
public sealed class WasmJsGlobal
{
    private readonly IWasmGlobal _global;

    internal WasmJsGlobal(IWasmGlobal global)
    {
        _global = global;
    }

    /// <summary>
    /// Gets or sets the global value.
    /// </summary>
    [DomName("value")]
    [DomAccessor(Accessors.Getter | Accessors.Setter)]
    public object? Value
    {
        get => Wrap(_global.GetValue());
        set => _global.SetValue(Unwrap(value));
    }

    /// <summary>
    /// Gets the primitive global value.
    /// </summary>
    /// <returns>The current value.</returns>
    [DomName("valueOf")]
    public object? ValueOf() => Value;

    private static object? Wrap(object? value) =>
        value is IWasmFunction function ? new WasmJsExportedFunction(function) : value;

    private static object? Unwrap(object? value) =>
        value is WasmJsExportedFunction { Function: not null } function ? function.Function : value;
}