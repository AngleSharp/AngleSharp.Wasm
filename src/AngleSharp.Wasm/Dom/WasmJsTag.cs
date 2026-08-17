namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using System;

/// <summary>
/// JavaScript-visible WebAssembly exception tag.
/// </summary>
[DomName("Tag")]
public sealed class WasmJsTag
{
    private readonly WasmJsTagType _type;

    /// <summary>
    /// Creates a WebAssembly exception tag.
    /// </summary>
    /// <param name="parameters">The ordered payload value types.</param>
    public WasmJsTag(string[] parameters)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (!IsSupportedParameter(parameters[i]))
            {
                throw new ArgumentException(
                    $"Unsupported WebAssembly tag parameter type '{parameters[i]}'.",
                    nameof(parameters));
            }
        }

        _type = new WasmJsTagType(parameters);
    }

    internal string[] Parameters => _type.Parameters;

    /// <summary>
    /// Gets the tag type descriptor.
    /// </summary>
    /// <returns>The tag type.</returns>
    [DomName("type")]
    public WasmJsTagType Type() => _type;

    private static bool IsSupportedParameter(string? parameter) => parameter is
        "i32" or "i64" or "f32" or "f64" or "v128" or
        "funcref" or "externref" or "anyref" or "eqref" or
        "i31ref" or "structref" or "arrayref" or "exnref";
}

/// <summary>
/// Describes a WebAssembly exception tag type.
/// </summary>
[DomName("TagType")]
public sealed class WasmJsTagType
{
    private readonly string[] _parameters;

    internal WasmJsTagType(string[] parameters)
    {
        _parameters = (string[])parameters.Clone();
    }

    /// <summary>
    /// Gets the tag parameter value types.
    /// </summary>
    [DomName("parameters")]
    [DomAccessor(Accessors.Getter)]
    public string[] Parameters => (string[])_parameters.Clone();
}