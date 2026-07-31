namespace AngleSharp.Wasm;

/// <summary>
/// Creates runtime instances for a browsing context.
/// </summary>
public interface IWasmRuntimeFactory
{
    /// <summary>
    /// Creates a new runtime instance.
    /// </summary>
    /// <param name="context">The active browsing context.</param>
    /// <returns>The runtime instance.</returns>
    IWasmRuntime Create(IBrowsingContext context);
}
