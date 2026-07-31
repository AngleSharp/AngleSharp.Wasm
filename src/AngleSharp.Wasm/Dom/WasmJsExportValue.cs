namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;

/// <summary>
/// JavaScript-visible non-function export entry.
/// </summary>
[DomName("ExportedValue")]
public sealed class WasmJsExportValue
{
    internal WasmJsExportValue(string name, string kind)
    {
        Name = name;
        Kind = kind;
    }

    /// <summary>
    /// Gets the export name.
    /// </summary>
    [DomName("name")]
    [DomAccessor(Accessors.Getter)]
    public string Name { get; }

    /// <summary>
    /// Gets the export kind.
    /// </summary>
    [DomName("kind")]
    [DomAccessor(Accessors.Getter)]
    public string Kind { get; }
}
