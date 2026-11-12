namespace AngleSharp.Wasm;

using System;

/// <summary>
/// Creates Wasmtime-backed runtime instances.
/// </summary>
public sealed class WasmtimeWasmRuntimeFactory : IWasmRuntimeFactory
{
    private readonly WasmRuntimeOptions _options;

    /// <summary>
    /// Creates a factory for runtimes without resource limits.
    /// </summary>
    public WasmtimeWasmRuntimeFactory()
        : this(new WasmRuntimeOptions())
    {
    }

    /// <summary>
    /// Creates a factory for runtimes with the given resource limits.
    /// </summary>
    /// <param name="options">The resource limits, read by every runtime this factory creates.</param>
    public WasmtimeWasmRuntimeFactory(WasmRuntimeOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public IWasmRuntime Create(IBrowsingContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return new WasmtimeWasmRuntime(_options);
    }
}
