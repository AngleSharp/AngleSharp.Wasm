namespace AngleSharp.Wasm;

/// <summary>
/// Represents a WebAssembly table.
/// </summary>
public interface IWasmTable
{
    /// <summary>
    /// Gets the current number of elements.
    /// </summary>
    ulong Length { get; }

    /// <summary>
    /// Gets an element by index.
    /// </summary>
    object? Get(uint index);

    /// <summary>
    /// Sets an element by index.
    /// </summary>
    void Set(uint index, object? value);

    /// <summary>
    /// Grows the table and initializes the new elements.
    /// </summary>
    /// <param name="delta">The number of elements to add.</param>
    /// <param name="value">The initial value for new elements.</param>
    /// <returns>The previous table length.</returns>
    ulong Grow(uint delta, object? value);
}