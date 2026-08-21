namespace AngleSharp.Wasm;

/// <summary>
/// Provides access to exported WebAssembly globals.
/// </summary>
public interface IWasmGlobalProvider
{
    /// <summary>
    /// Gets an exported global by name.
    /// </summary>
    /// <param name="exportName">The export name.</param>
    /// <returns>The exported global, if present.</returns>
    IWasmGlobal? GetGlobal(string exportName);
}