namespace AngleSharp.Wasm;

/// <summary>
/// Provides access to exported WebAssembly tables.
/// </summary>
public interface IWasmTableProvider
{
    /// <summary>
    /// Gets an exported table by name.
    /// </summary>
    /// <param name="exportName">The export name.</param>
    /// <returns>The exported table, if present.</returns>
    IWasmTable? GetTable(string exportName);
}