namespace AngleSharp.Wasm;

using System;

/// <summary>
/// Represents a WebAssembly linear memory.
/// </summary>
public interface IWasmMemory
{
    /// <summary>
    /// Gets the current memory length in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Copies bytes from memory into a destination span.
    /// </summary>
    /// <param name="offset">The byte offset in memory.</param>
    /// <param name="destination">The destination span.</param>
    void Read(long offset, Span<byte> destination);

    /// <summary>
    /// Copies bytes from a source span into memory.
    /// </summary>
    /// <param name="offset">The byte offset in memory.</param>
    /// <param name="source">The source span.</param>
    void Write(long offset, ReadOnlySpan<byte> source);

    /// <summary>
    /// Grows the memory by a number of WebAssembly pages.
    /// </summary>
    /// <param name="delta">The number of pages to add.</param>
    /// <returns>The previous size in pages.</returns>
    long Grow(long delta);
}