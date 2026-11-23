namespace AngleSharp.Wasm.Dom;

using System;
using AngleSharp.Attributes;
using AngleSharp.Dom;
using Wasmtime;

/// <summary>
/// A linear memory of WebAssembly (WebAssembly.Memory), whose buffer is an ArrayBuffer
/// sharing its bytes.
/// </summary>
[DomName("Memory")]
[DomExposed("WebAssembly")]
public sealed class WasmJsMemory
{
    private const long PageSize = 65536;

    private readonly IWasmMemory _memory;
    private readonly WasmJsRealm? _realm;

    internal WasmJsMemory(IWasmMemory memory)
    {
        _memory = memory;
    }

    internal WasmJsMemory(WasmJsRealm realm, WasmtimeMemory memory)
    {
        _memory = memory;
        _realm = realm;
    }

    /// <summary>
    /// Creates a memory (new WebAssembly.Memory).
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="descriptor">The initial and maximum size in pages.</param>
    [DomConstructor]
    public WasmJsMemory(IWindow window, WasmJsMemoryDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }
        else if (descriptor.Shared)
        {
            throw new NotSupportedException("Shared memories are not supported.");
        }

        _realm = WebAssembly.GetRealm(window);
        var maximum = descriptor.Maximum is double max ? (long?)ToSize(max) : null;
        _memory = new WasmtimeMemory(_realm.CreateMemory(ToSize(descriptor.Initial), maximum));
    }

    internal Memory? Native => (_memory as WasmtimeMemory)?.Native;

    /// <summary>
    /// Gets a copy of the bytes of the memory.
    /// </summary>
    public byte[] Buffer => Read(0, checked((int)_memory.Length));

    /// <summary>
    /// Gets the bytes of the memory as scripts see them (memory.buffer): an ArrayBuffer sharing
    /// them, which is a new one once the memory has grown, as in browsers.
    /// </summary>
    [DomName("buffer")]
    [DomAccessor(Accessors.Getter)]
    public object ScriptBuffer
    {
        get
        {
            if (Native is Memory memory && HostMemory.Find(memory.GetPointer(), memory.GetLength()) is byte[] bytes)
            {
                return new Memory<byte>(bytes);
            }

            // the memory isn't in an array of .NET, so scripts get what is there at the moment
            return Buffer;
        }
    }

    /// <summary>
    /// Reads bytes of the memory.
    /// </summary>
    /// <param name="offset">The address.</param>
    /// <param name="count">The number of bytes.</param>
    /// <returns>The bytes.</returns>
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
    /// Writes bytes to the memory.
    /// </summary>
    /// <param name="offset">The address.</param>
    /// <param name="bytes">The bytes.</param>
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
    /// Grows the memory (memory.grow).
    /// </summary>
    /// <param name="delta">The number of pages to add.</param>
    /// <returns>The size in pages before.</returns>
    [DomName("grow")]
    public long Grow(long delta)
    {
        if (delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        try
        {
            return _memory.Grow(delta);
        }
        catch (Exception exception) when (exception is Wasmtime.WasmtimeException)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), exception.Message);
        }
    }

    private static long ToSize(double pages) =>
        pages >= 0 && pages <= 65536 && pages == Math.Floor(pages)
            ? (long)pages
            : throw new ArgumentOutOfRangeException(nameof(pages), "The size has to be a number of pages from 0 to 65536.");

    private void EnsureRange(long offset, int count)
    {
        if (offset < 0 || offset > _memory.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }
}
