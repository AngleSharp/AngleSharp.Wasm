namespace AngleSharp.Wasm;

/// <summary>
/// Provides access to exported WebAssembly memories.
/// </summary>
public interface IWasmMemoryProvider
{
    /// <summary>
    /// Gets an exported memory by name.
    /// </summary>
    /// <param name="exportName">The export name.</param>
    /// <returns>The exported memory, if present.</returns>
    IWasmMemory? GetMemory(string exportName);
}