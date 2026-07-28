namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// Minimal JavaScript bridge object exposed as WebAssembly in compatible JS engines.
/// </summary>
[DomName("WebAssembly")]
[DomNoInterfaceObject]
[DomExposed("Window")]
public static class WebAssembly
{
    private static readonly ConditionalWeakTable<IBrowsingContext, IWasmRuntime> runtimeCache = new ();

    /// <summary>
    /// Compiles a module and returns a bridge module handle.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <returns>The module handle.</returns>
    [DomName("compile")]
    public static WasmJsModule Compile(this IWindow window, byte[] moduleBytes)
    {
        if (moduleBytes is null)
        {
            throw new ArgumentNullException(nameof(moduleBytes));
        }

        var runtime = GetOrCreateRuntime(window);
        var module = runtime.CompileAsync(moduleBytes).AsTask().GetAwaiter().GetResult();
        return new WasmJsModule(runtime, module, moduleBytes);
    }

    /// <summary>
    /// Instantiates a module handle and returns a bridge instance.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="module">The compiled module handle.</param>
    /// <returns>The instance handle.</returns>
    [DomName("instantiate")]
    public static WasmJsInstance Instantiate(this IWindow window, WasmJsModule module)
    {
        if (module is null)
        {
            throw new ArgumentNullException(nameof(module));
        }

        var imports = GetImports(window);
        var instance = module.Runtime.InstantiateAsync(module.Module, imports).AsTask().GetAwaiter().GetResult();
        return new WasmJsInstance(instance, module.ExportsMetadata);
    }

    private static IWasmRuntime GetOrCreateRuntime(IWindow window) =>
        runtimeCache.GetValue(window.Document?.Context!, static context =>
        {
            var factory = context.GetService<IWasmRuntimeFactory>()
                ?? throw new InvalidOperationException("No IWasmRuntimeFactory has been registered.");
            return factory.Create(context);
        });

    private static IReadOnlyList<WasmImportFunction> GetImports(IWindow window) =>
        window.Document.Context
            .GetServices<IWasmImportProvider>()
            .SelectMany(provider => provider.GetImports(window.Document.Context))
            .ToArray();
}

