namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using AngleSharp.Dom;
using AngleSharp.Io;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    /// Determines whether bytes form a valid WebAssembly module.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <returns>True if the module is valid; otherwise, false.</returns>
    [DomName("validate")]
    public static bool Validate(this IWindow window, byte[] moduleBytes)
    {
        if (moduleBytes is null)
        {
            throw new ArgumentNullException(nameof(moduleBytes));
        }

        var runtime = GetOrCreateRuntime(window);

        if (runtime is IWasmValidator validator)
        {
            return validator.Validate(moduleBytes);
        }

        try
        {
            using var module = runtime.CompileAsync(moduleBytes).AsTask().GetAwaiter().GetResult();
            return true;
        }
        catch (Exception exception) when (exception is ObjectDisposedException or OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Compiles a WebAssembly module from an HTTP response body.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response containing the module.</param>
    /// <returns>The compiled module.</returns>
    [DomName("compileStreaming")]
    public static WasmJsModule CompileStreaming(this IWindow window, IResponse source) =>
        CompileStreaming(window, source, null);

    /// <summary>
    /// Compiles a WebAssembly module from an HTTP response body with compile options.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response containing the module.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The compiled module.</returns>
    [DomName("compileStreaming")]
    public static WasmJsModule CompileStreaming(this IWindow window, IResponse source, WasmCompileOptions? options) =>
        Compile(window, ReadStreamingSource(source), options);

    /// <summary>
    /// Compiles and instantiates a WebAssembly module from an HTTP response body.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response containing the module.</param>
    /// <returns>The compiled module and instance.</returns>
    [DomName("instantiateStreaming")]
    public static WasmJsInstantiationResult InstantiateStreaming(this IWindow window, IResponse source)
        => InstantiateStreaming(window, source, null);

    /// <summary>
    /// Compiles and instantiates a WebAssembly module from an HTTP response body with compile options.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response containing the module.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The compiled module and instance.</returns>
    [DomName("instantiateStreaming")]
    public static WasmJsInstantiationResult InstantiateStreaming(
        this IWindow window,
        IResponse source,
        WasmCompileOptions? options)
    {
        var module = CompileStreaming(window, source, options);
        var instance = Instantiate(window, module);
        return new WasmJsInstantiationResult(module, instance);
    }

    /// <summary>
    /// Compiles and instantiates module bytes.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <returns>The compiled module and instance.</returns>
    [DomName("instantiate")]
    public static WasmJsInstantiationResult Instantiate(this IWindow window, byte[] moduleBytes) =>
        Instantiate(window, moduleBytes, null);

    /// <summary>
    /// Compiles and instantiates module bytes with compile options.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The compiled module and instance.</returns>
    [DomName("instantiate")]
    public static WasmJsInstantiationResult Instantiate(
        this IWindow window,
        byte[] moduleBytes,
        WasmCompileOptions? options)
    {
        var module = Compile(window, moduleBytes, options);
        var instance = Instantiate(window, module);
        return new WasmJsInstantiationResult(module, instance);
    }

    /// <summary>
    /// Compiles a module and returns a bridge module handle.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <returns>The module handle.</returns>
    [DomName("compile")]
    public static WasmJsModule Compile(this IWindow window, byte[] moduleBytes)
        => Compile(window, moduleBytes, null);

    /// <summary>
    /// Compiles a module with compile options and returns a bridge module handle.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The module handle.</returns>
    [DomName("compile")]
    public static WasmJsModule Compile(this IWindow window, byte[] moduleBytes, WasmCompileOptions? options)
    {
        if (moduleBytes is null)
        {
            throw new ArgumentNullException(nameof(moduleBytes));
        }

        var runtime = GetOrCreateRuntime(window);
        var compileOptions = options?.Snapshot();
        IWasmCompiledModule module;

        if (compileOptions is not null && runtime is IWasmCompileOptionsCompiler compiler)
        {
            module = compiler.CompileAsync(moduleBytes, compileOptions).AsTask().GetAwaiter().GetResult();
        }
        else
        {
            if (compileOptions?.HasFeatures == true)
            {
                throw new NotSupportedException("The configured WebAssembly runtime does not support compile options.");
            }

            module = runtime.CompileAsync(moduleBytes).AsTask().GetAwaiter().GetResult();
        }

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

    private static byte[] ReadStreamingSource(IResponse source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source.StatusCode < HttpStatusCode.OK || source.StatusCode >= HttpStatusCode.MultipleChoices)
        {
            throw new InvalidOperationException($"The WebAssembly response has unsuccessful status code {(int)source.StatusCode}.");
        }

        var contentType = source.Headers
            .FirstOrDefault(header => String.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
            .Value;

        if (contentType is null || !String.Equals(
            contentType.Split(';', 2)[0].Trim(),
            "application/wasm",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The WebAssembly response must use the 'application/wasm' content type.");
        }

        using var buffer = new MemoryStream();
        source.Content.CopyTo(buffer);
        return buffer.ToArray();
    }
}

