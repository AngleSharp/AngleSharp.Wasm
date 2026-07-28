namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;

/// <summary>
/// JavaScript-visible instance handle.
/// </summary>
[DomName("Instance")]
public sealed class WasmJsInstance
{
    internal WasmJsInstance(IWasmInstance instance)
    {
        Instance = instance;
    }

    internal IWasmInstance Instance { get; }

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
