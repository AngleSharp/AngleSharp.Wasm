namespace AngleSharp.Wasm;

using System;
using Wasmtime;

internal sealed class WasmtimeMemory : IWasmMemory
{
    private readonly Memory _memory;

    public WasmtimeMemory(Memory memory)
    {
        _memory = memory;
    }

    public long Length => _memory.GetLength();

    public void Read(long offset, Span<byte> destination) =>
        _memory.GetSpan(offset, destination.Length).CopyTo(destination);

    public void Write(long offset, ReadOnlySpan<byte> source) =>
        source.CopyTo(_memory.GetSpan(offset, source.Length));

    public long Grow(long delta) => _memory.Grow(delta);
}