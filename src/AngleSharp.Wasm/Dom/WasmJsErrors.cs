namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using System;

/// <summary>
/// Represents a WebAssembly compilation failure.
/// </summary>
[DomName("CompileError")]
public sealed class WasmJsCompileError : Exception
{
    /// <summary>
    /// Gets the standard error name.
    /// </summary>
    [DomName("name")]
    [DomAccessor(Accessors.Getter)]
    public string Name => "CompileError";

    /// <summary>
    /// Creates an empty compilation error.
    /// </summary>
    public WasmJsCompileError()
        : base(String.Empty)
    {
    }

    /// <summary>
    /// Creates a compilation error with a message.
    /// </summary>
    public WasmJsCompileError(string? message)
        : base(message)
    {
    }

    internal WasmJsCompileError(string? message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents a WebAssembly instantiation or linking failure.
/// </summary>
[DomName("LinkError")]
public sealed class WasmJsLinkError : Exception
{
    /// <summary>
    /// Gets the standard error name.
    /// </summary>
    [DomName("name")]
    [DomAccessor(Accessors.Getter)]
    public string Name => "LinkError";

    /// <summary>
    /// Creates an empty linking error.
    /// </summary>
    public WasmJsLinkError()
        : base(String.Empty)
    {
    }

    /// <summary>
    /// Creates a linking error with a message.
    /// </summary>
    public WasmJsLinkError(string? message)
        : base(message)
    {
    }

    internal WasmJsLinkError(string? message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents a WebAssembly execution failure.
/// </summary>
[DomName("RuntimeError")]
public sealed class WasmJsRuntimeError : Exception
{
    /// <summary>
    /// Gets the standard error name.
    /// </summary>
    [DomName("name")]
    [DomAccessor(Accessors.Getter)]
    public string Name => "RuntimeError";

    /// <summary>
    /// Creates an empty runtime error.
    /// </summary>
    public WasmJsRuntimeError()
        : base(String.Empty)
    {
    }

    /// <summary>
    /// Creates a runtime error with a message.
    /// </summary>
    public WasmJsRuntimeError(string? message)
        : base(message)
    {
    }

    internal WasmJsRuntimeError(string? message, Exception innerException)
        : base(message, innerException)
    {
    }
}