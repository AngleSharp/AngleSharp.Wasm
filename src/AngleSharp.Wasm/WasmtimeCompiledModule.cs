namespace AngleSharp.Wasm;

using System;
using Wasmtime;

/// <summary>
/// Wraps a compiled Wasmtime module.
/// </summary>
public sealed class WasmtimeCompiledModule : IWasmCompiledModule
{
    private bool _disposed;

    internal WasmtimeCompiledModule(Engine engine, Module module)
    {
        Engine = engine;
        Module = module;
    }

    internal Engine Engine { get; }

    internal Module Module { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Module.Dispose();
    }
}
