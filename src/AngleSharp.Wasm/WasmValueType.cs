namespace AngleSharp.Wasm;

/// <summary>
/// Represents the primitive value types shared across supported WebAssembly runtimes.
/// </summary>
public enum WasmValueType
{
    /// <summary>
    /// 32-bit integer.
    /// </summary>
    Int32,

    /// <summary>
    /// 64-bit integer.
    /// </summary>
    Int64,

    /// <summary>
    /// 32-bit floating-point number.
    /// </summary>
    Float32,

    /// <summary>
    /// 64-bit floating-point number.
    /// </summary>
    Float64,
}
