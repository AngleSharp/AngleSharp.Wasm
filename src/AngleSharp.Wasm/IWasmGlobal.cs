namespace AngleSharp.Wasm;

/// <summary>
/// Represents a WebAssembly global.
/// </summary>
public interface IWasmGlobal
{
    /// <summary>
    /// Gets a value indicating whether the global is mutable.
    /// </summary>
    bool IsMutable { get; }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    object? GetValue();

    /// <summary>
    /// Sets the current value.
    /// </summary>
    /// <param name="value">The new value.</param>
    void SetValue(object? value);
}