namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;

/// <summary>
/// JavaScript-visible WebAssembly exception tag.
/// </summary>
[DomName("Tag")]
public sealed class WasmJsTag
{
    private readonly WasmJsTagType _type;

    internal WasmJsTag(string[] parameters)
    {
        _type = new WasmJsTagType(parameters);
    }

    /// <summary>
    /// Gets the tag type descriptor.
    /// </summary>
    /// <returns>The tag type.</returns>
    [DomName("type")]
    public WasmJsTagType Type() => _type;
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