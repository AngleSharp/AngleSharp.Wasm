namespace AngleSharp.Wasm;

using System.Collections.Generic;

/// <summary>
/// Provides host imports to use for WebAssembly instantiation.
/// </summary>
public interface IWasmImportProvider
{
    /// <summary>
    /// Gets the imports available in the current browsing context.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <returns>The available import functions.</returns>
    IEnumerable<WasmImportFunction> GetImports(IBrowsingContext context);
}
