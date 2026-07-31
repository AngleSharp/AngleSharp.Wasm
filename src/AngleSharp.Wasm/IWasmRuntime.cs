namespace AngleSharp.Wasm;

using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a runtime capable of compiling and instantiating WebAssembly modules.
/// </summary>
public interface IWasmRuntime : IDisposable
{
    /// <summary>
    /// Compiles a WebAssembly binary module.
    /// </summary>
    /// <param name="moduleBytes">The raw WebAssembly bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compiled module.</returns>
    ValueTask<IWasmCompiledModule> CompileAsync(ReadOnlyMemory<byte> moduleBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Instantiates a previously compiled WebAssembly module.
    /// </summary>
    /// <param name="compiledModule">The compiled module to instantiate.</param>
    /// <param name="imports">The host imports to expose during instantiation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created module instance.</returns>
    ValueTask<IWasmInstance> InstantiateAsync(
        IWasmCompiledModule compiledModule,
        IEnumerable<WasmImportFunction>? imports = null,
        CancellationToken cancellationToken = default);
}
