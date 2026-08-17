namespace AngleSharp.Wasm;

/// <summary>
/// Provides access to exported WebAssembly functions.
/// </summary>
public interface IWasmFunctionProvider
{
    /// <summary>
    /// Gets an exported function by name.
    /// </summary>
    /// <param name="exportName">The export name.</param>
    /// <returns>The exported function, if present.</returns>
    IWasmFunction? GetFunction(string exportName);
}