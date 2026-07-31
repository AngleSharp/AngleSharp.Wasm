namespace AngleSharp.Wasm;

using System;

/// <summary>
/// Creates Wasmtime-backed runtime instances.
/// </summary>
public sealed class WasmtimeWasmRuntimeFactory : IWasmRuntimeFactory
{
    /// <inheritdoc />
    public IWasmRuntime Create(IBrowsingContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return new WasmtimeWasmRuntime();
    }
}
