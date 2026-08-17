namespace AngleSharp.Wasm;

/// <summary>
/// Represents a WebAssembly function reference.
/// </summary>
public interface IWasmFunction
{
    /// <summary>
    /// Invokes the function.
    /// </summary>
    /// <param name="arguments">The arguments to pass.</param>
    /// <returns>The invocation result, if any.</returns>
    object? Invoke(object?[]? arguments = null);
}