namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using System;

/// <summary>
/// JavaScript-visible WebAssembly linear memory.
/// </summary>
[DomName("Memory")]
public sealed class WasmJsMemory
{
    private readonly IWasmMemory _memory;

    internal WasmJsMemory(IWasmMemory memory)
    {
        _memory = memory;
    }

    /// <summary>
    /// Gets a snapshot of the current memory contents.
    /// </summary>
    [DomName("buffer")]
    [DomAccessor(Accessors.Getter)]
    public byte[] Buffer => Read(0, checked((int)_memory.Length));

    /// <summary>
    /// Reads bytes from memory.
    /// </summary>
    /// <param name="offset">The byte offset in memory.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>A copy of the requested bytes.</returns>
    [DomName("read")]
    public byte[] Read(long offset, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        EnsureRange(offset, count);
        var bytes = new byte[count];
        _memory.Read(offset, bytes);
        return bytes;
    }

    /// <summary>
    /// Writes bytes into memory.
    /// </summary>
    /// <param name="offset">The byte offset in memory.</param>
    /// <param name="bytes">The bytes to write.</param>
    [DomName("write")]
    public void Write(long offset, byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        EnsureRange(offset, bytes.Length);
        _memory.Write(offset, bytes);
    }

    /// <summary>
    /// Grows the memory by a number of WebAssembly pages.
    /// </summary>
    /// <param name="delta">The number of pages to add.</param>
    /// <returns>The previous size in pages.</returns>
    [DomName("grow")]
    public long Grow(long delta)
    {
        if (delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        return _memory.Grow(delta);
    }

    private void EnsureRange(long offset, int count)
    {
        if (offset < 0 || offset > _memory.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }
}