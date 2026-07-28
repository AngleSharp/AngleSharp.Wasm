namespace AngleSharp.Wasm;

using System;
using System.Threading;
using System.Threading.Tasks;
using Wasmtime;

/// <summary>
/// Wasmtime-backed <see cref="IWasmRuntime"/> implementation.
/// </summary>
public sealed class WasmtimeWasmRuntime : IWasmRuntime
{
    private readonly Engine _engine;
    private bool _disposed;

    /// <summary>
    /// Creates a new runtime.
    /// </summary>
    public WasmtimeWasmRuntime()
    {
        _engine = new Engine();
    }

    /// <inheritdoc />
    public ValueTask<IWasmCompiledModule> CompileAsync(ReadOnlyMemory<byte> moduleBytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var module = Module.FromBytes(_engine, "anglesharp-wasm", moduleBytes.ToArray());
        return ValueTask.FromResult<IWasmCompiledModule>(new WasmtimeCompiledModule(_engine, module));
    }

    /// <inheritdoc />
    public ValueTask<IWasmInstance> InstantiateAsync(IWasmCompiledModule compiledModule, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (compiledModule is not WasmtimeCompiledModule wasmtimeModule)
        {
            throw new ArgumentException("Compiled module must be created by WasmtimeWasmRuntime.", nameof(compiledModule));
        }

        var linker = new Linker(wasmtimeModule.Engine);
        var store = new Store(wasmtimeModule.Engine);
        var instance = linker.Instantiate(store, wasmtimeModule.Module);

        return ValueTask.FromResult<IWasmInstance>(new WasmtimeInstance(store, instance));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmtimeWasmRuntime));
        }
    }
}
