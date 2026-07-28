namespace AngleSharp;

using AngleSharp.Wasm;
using System;

/// <summary>
/// Additional extensions to register a WebAssembly runtime.
/// </summary>
public static class WasmConfigurationExtensions
{
    /// <summary>
    /// Registers the default Wasmtime-backed WebAssembly runtime factory.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The updated configuration.</returns>
    public static IConfiguration WithWasm(this IConfiguration configuration) =>
        configuration.WithWasm(context => new WasmtimeWasmRuntimeFactory());

    /// <summary>
    /// Registers a custom WebAssembly runtime factory.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="factory">The factory creator delegate.</param>
    /// <returns>The updated configuration.</returns>
    public static IConfiguration WithWasm(this IConfiguration configuration, Func<IBrowsingContext, IWasmRuntimeFactory> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return configuration.WithOnly(factory);
    }
}
