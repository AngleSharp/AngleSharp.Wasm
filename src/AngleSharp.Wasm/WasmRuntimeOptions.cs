namespace AngleSharp.Wasm;

/// <summary>
/// Optional resource limits for a WebAssembly runtime, honored by the default
/// <see cref="WasmtimeWasmRuntime"/>. Every limit is off by default; set them when the runtime
/// executes modules from untrusted content.
/// </summary>
public sealed class WasmRuntimeOptions
{
    /// <summary>
    /// Gets or sets the maximum size in bytes that a single linear memory may have. Instantiating
    /// a module whose memory starts larger fails, and growing a memory beyond it fails.
    /// <c>null</c> (the default) means no limit beyond the WebAssembly maximum.
    /// </summary>
    public long? MaximumMemorySize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of elements that a single table may have. Instantiating a
    /// module whose table starts larger fails, and growing a table beyond it fails.
    /// <c>null</c> (the default) means no limit beyond the WebAssembly maximum.
    /// </summary>
    public uint? MaximumTableElements { get; set; }

    /// <summary>
    /// Gets or sets the fuel that WebAssembly code may consume per call from the host into an
    /// instance, and per instantiation (start function). Fuel is roughly one unit per executed
    /// WebAssembly instruction; when it runs out the call traps. Calls that WebAssembly makes back
    /// into its own instance from a host import share the budget of the outer call.
    /// <c>null</c> (the default) means no limit; setting it enables fuel metering, which slows
    /// down execution.
    /// </summary>
    public ulong? Fuel { get; set; }
}
