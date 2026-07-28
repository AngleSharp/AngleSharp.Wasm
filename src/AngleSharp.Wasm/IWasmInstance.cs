namespace AngleSharp.Wasm;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents an instantiated WebAssembly module.
/// </summary>
public interface IWasmInstance : IDisposable
{
    /// <summary>
    /// Invokes an exported function by name.
    /// </summary>
    /// <param name="exportName">The export name.</param>
    /// <param name="arguments">The arguments to pass.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invocation result, if any.</returns>
    ValueTask<object?> InvokeAsync(string exportName, object?[]? arguments = null, CancellationToken cancellationToken = default);
}
