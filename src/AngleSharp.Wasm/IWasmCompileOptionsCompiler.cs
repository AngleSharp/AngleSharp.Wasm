namespace AngleSharp.Wasm;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Compiles WebAssembly modules with compile options.
/// </summary>
public interface IWasmCompileOptionsCompiler
{
    /// <summary>
    /// Compiles a WebAssembly binary module with the supplied options.
    /// </summary>
    /// <param name="moduleBytes">The raw WebAssembly bytes.</param>
    /// <param name="options">The compile options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compiled module.</returns>
    ValueTask<IWasmCompiledModule> CompileAsync(
        ReadOnlyMemory<byte> moduleBytes,
        WasmCompileOptions options,
        CancellationToken cancellationToken = default);
}