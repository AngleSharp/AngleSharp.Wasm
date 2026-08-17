namespace AngleSharp.Wasm;

using System;

/// <summary>
/// Validates WebAssembly binary modules without compiling them.
/// </summary>
public interface IWasmValidator
{
    /// <summary>
    /// Determines whether the supplied bytes form a valid WebAssembly module.
    /// </summary>
    /// <param name="moduleBytes">The raw WebAssembly bytes.</param>
    /// <returns>True if the module is valid; otherwise, false.</returns>
    bool Validate(ReadOnlyMemory<byte> moduleBytes);
}