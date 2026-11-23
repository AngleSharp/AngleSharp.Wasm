namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using AngleSharp.Dom;
using AngleSharp.Io;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// The WebAssembly namespace of the JavaScript API: compiling and instantiating modules, which
/// gives promises, and the interfaces of its objects (WebAssembly.Module, Instance, Memory, ...).
/// </summary>
[DomName("WebAssembly")]
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
    public static WasmJsModule CompileStreaming(this IWindow window, IResponse source) =>
        CompileStreaming(window, source, null);

    /// <summary>
    /// Compiles a WebAssembly module from an HTTP response body with compile options.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response containing the module.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The compiled module.</returns>
    public static WasmJsModule CompileStreaming(this IWindow window, IResponse source, WasmCompileOptions? options) =>
        Compile(window, ReadStreamingSource(source), options);

    /// <summary>
    /// Compiles and instantiates a WebAssembly module from an HTTP response body.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response containing the module.</param>
    /// <returns>The compiled module and instance.</returns>
    public static WasmJsInstantiationResult InstantiateStreaming(this IWindow window, IResponse source)
        => InstantiateStreaming(window, source, null);

    /// <summary>
    /// Compiles and instantiates a WebAssembly module from an HTTP response body with compile options.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response containing the module.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The compiled module and instance.</returns>
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
    public static WasmJsInstantiationResult Instantiate(this IWindow window, byte[] moduleBytes) =>
        Instantiate(window, moduleBytes, null);

    /// <summary>
    /// Compiles and instantiates module bytes with compile options.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The compiled module and instance.</returns>
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
    public static WasmJsModule Compile(this IWindow window, byte[] moduleBytes)
        => Compile(window, moduleBytes, null);

    /// <summary>
    /// Compiles a module with compile options and returns a bridge module handle.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="moduleBytes">The module bytes.</param>
    /// <param name="options">The compile options.</param>
    /// <returns>The module handle.</returns>
    public static WasmJsModule Compile(this IWindow window, byte[] moduleBytes, WasmCompileOptions? options)
    {
        if (moduleBytes is null)
        {
            throw new ArgumentNullException(nameof(moduleBytes));
        }

        var runtime = GetOrCreateRuntime(window);
        var compileOptions = options?.Snapshot();
        IWasmCompiledModule module;

        try
        {
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
        catch (Exception exception) when (ShouldTranslateCompileFailure(exception))
        {
            throw new WasmJsCompileError(exception.Message, exception);
        }
    }

    /// <summary>
    /// Instantiates a module handle and returns a bridge instance.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="module">The compiled module handle.</param>
    /// <returns>The instance handle.</returns>
    public static WasmJsInstance Instantiate(this IWindow window, WasmJsModule module)
    {
        if (module is null)
        {
            throw new ArgumentNullException(nameof(module));
        }

        try
        {
            var imports = GetImports(window);
            var instance = module.Runtime.InstantiateAsync(module.Module, imports).AsTask().GetAwaiter().GetResult();
            return new WasmJsInstance(instance, module.ExportsMetadata);
        }
        catch (Exception exception) when (ShouldTranslateOperationFailure(exception))
        {
            throw new WasmJsLinkError(exception.Message, exception);
        }
    }

    /// <summary>
    /// Compiles a module (WebAssembly.compile), which rejects with a CompileError if the bytes
    /// are no valid module.
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="bytes">The bytes of the module.</param>
    /// <returns>The module.</returns>
    [DomName("compile")]
    public static Task<WasmJsModule> CompileAsync(this IWindow window, byte[] bytes) =>
        Settle(() => Compile(window, bytes));

    /// <summary>
    /// Compiles and instantiates a module (WebAssembly.instantiate).
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="bytes">The bytes of the module.</param>
    /// <param name="importObject">What the module imports, by module and name.</param>
    /// <returns>The module and its instance.</returns>
    [DomName("instantiate")]
    public static Task<WasmJsInstantiationResult> InstantiateAsync(this IWindow window, byte[] bytes, IDictionary<string, IDictionary<string, WasmJsImportValue>>? importObject = null) =>
        Settle(() =>
        {
            var module = Compile(window, bytes);
            return new WasmJsInstantiationResult(module, InstantiateCore(window, module, importObject));
        });

    /// <summary>
    /// Instantiates a compiled module (WebAssembly.instantiate).
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="module">The module.</param>
    /// <param name="importObject">What the module imports, by module and name.</param>
    /// <returns>The instance.</returns>
    [DomName("instantiate")]
    public static Task<WasmJsInstance> InstantiateAsync(this IWindow window, WasmJsModule module, IDictionary<string, IDictionary<string, WasmJsImportValue>>? importObject = null) =>
        Settle(() => InstantiateCore(window, module, importObject));

    /// <summary>
    /// Compiles the module of a response (WebAssembly.compileStreaming), e.g. of fetch().
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response, or a promise of it.</param>
    /// <returns>The module.</returns>
    [DomName("compileStreaming")]
    public static async Task<WasmJsModule> CompileStreamingAsync(this IWindow window, Task<object> source) =>
        Compile(window, await ReadResponseAsync(await source));

    /// <summary>
    /// Compiles and instantiates the module of a response (WebAssembly.instantiateStreaming).
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="source">The response, or a promise of it.</param>
    /// <param name="importObject">What the module imports, by module and name.</param>
    /// <returns>The module and its instance.</returns>
    [DomName("instantiateStreaming")]
    public static async Task<WasmJsInstantiationResult> InstantiateStreamingAsync(this IWindow window, Task<object> source, IDictionary<string, IDictionary<string, WasmJsImportValue>>? importObject = null)
    {
        var module = Compile(window, await ReadResponseAsync(await source));
        return new WasmJsInstantiationResult(module, InstantiateCore(window, module, importObject));
    }

    internal static WasmJsInstance InstantiateCore(IWindow window, WasmJsModule module, IDictionary<string, IDictionary<string, WasmJsImportValue>>? importObject)
    {
        if (module is null)
        {
            throw new ArgumentNullException(nameof(module));
        }

        return GetRealm(window).Instantiate(module, importObject);
    }

    internal static WasmJsRealm GetRealm(IWindow window) => WasmJsRealm.Of(window, GetOrCreateRuntime(window));

    //  The work is done right away, but a failure rejects the promise rather than throwing.
    private static Task<T> Settle<T>(Func<T> action)
    {
        try
        {
            return Task.FromResult(action());
        }
        catch (Exception exception)
        {
            return Task.FromException<T>(exception);
        }
    }

    //  A Response of a fetch(), known by the names of its members (it is AngleSharp.Io's), or
    //  a response of AngleSharp.
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The members of a response are those of a DOM object, which the scripting keeps.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The members of a response are those of a DOM object, which the scripting keeps.")]
    private static async Task<byte[]> ReadResponseAsync(object? source)
    {
        if (source is IResponse response)
        {
            return ReadStreamingSource(response);
        }

        var type = source?.GetType() ?? throw new ArgumentException("The source has to be a Response.");
        var ok = FindMember(type, "ok") as PropertyInfo;
        var headers = (FindMember(type, "headers") as PropertyInfo)?.GetValue(source);
        var get = headers is null ? null : FindMember(headers.GetType(), "get") as MethodInfo;
        var arrayBuffer = FindMember(type, "arrayBuffer") as MethodInfo
            ?? throw new ArgumentException("The source has to be a Response.");

        if (ok?.GetValue(source) is false)
        {
            throw new ArgumentException("The response of the module was not successful.");
        }

        var contentType = get?.Invoke(headers, new object?[] { "Content-Type" }) as string;

        if (contentType is null || !String.Equals(contentType.Split(';', 2)[0].Trim(), "application/wasm", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The response of the module has to be of the type application/wasm.");
        }

        var reading = (Task)arrayBuffer.Invoke(source, Array.Empty<object?>())!;
        await reading.ConfigureAwait(false);
        return reading.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(reading) switch
        {
            byte[] bytes => bytes,
            ArraySegment<byte> segment => segment.ToArray(),
            _ => throw new ArgumentException("The body of the response could not be read."),
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The members of a response are those of a DOM object, which the scripting keeps.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The members of a response are those of a DOM object, which the scripting keeps.")]
    private static MemberInfo? FindMember(Type type, string name) =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(member => member.GetCustomAttributes<DomNameAttribute>().Any(attribute => attribute.OfficialName == name));

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

    internal static bool ShouldTranslateOperationFailure(Exception exception) => exception is not
        (ArgumentException or NotSupportedException or ObjectDisposedException or OperationCanceledException or
        WasmJsCompileError or WasmJsLinkError or WasmJsRuntimeError);

    private static bool ShouldTranslateCompileFailure(Exception exception) => exception is not
        (NotSupportedException or ObjectDisposedException or OperationCanceledException or
        WasmJsCompileError or WasmJsLinkError or WasmJsRuntimeError);
}

