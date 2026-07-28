namespace AngleSharp;

using AngleSharp.Wasm;
using System.Collections.Generic;
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

    /// <summary>
    /// Registers a provider for host import functions.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="provider">The import provider.</param>
    /// <returns>The updated configuration.</returns>
    public static IConfiguration WithWasmImports(this IConfiguration configuration, IWasmImportProvider provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        return configuration.With(provider);
    }

    /// <summary>
    /// Registers a delegate-based provider for host import functions.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="provider">The imports delegate.</param>
    /// <returns>The updated configuration.</returns>
    public static IConfiguration WithWasmImports(this IConfiguration configuration, Func<IBrowsingContext, IEnumerable<WasmImportFunction>> provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        return configuration.With<IWasmImportProvider>(context => new DelegateWasmImportProvider(provider));
    }

    private sealed class DelegateWasmImportProvider : IWasmImportProvider
    {
        private readonly Func<IBrowsingContext, IEnumerable<WasmImportFunction>> _provider;

        public DelegateWasmImportProvider(Func<IBrowsingContext, IEnumerable<WasmImportFunction>> provider)
        {
            _provider = provider;
        }

        public IEnumerable<WasmImportFunction> GetImports(IBrowsingContext context) => _provider(context);
    }
}
