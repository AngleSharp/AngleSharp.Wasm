namespace AngleSharp.Wasm;

using AngleSharp.Attributes;
using System;
using System.Collections.Generic;

/// <summary>
/// Configures optional WebAssembly compilation features.
/// </summary>
public sealed class WasmCompileOptions
{
    /// <summary>
    /// Gets or sets the requested JavaScript builtin sets.
    /// </summary>
    [DomName("builtins")]
    [DomAccessor(Accessors.Getter | Accessors.Setter)]
    public IReadOnlyList<string> Builtins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the module namespace for imported string constants.
    /// </summary>
    [DomName("importedStringConstants")]
    [DomAccessor(Accessors.Getter | Accessors.Setter)]
    public string? ImportedStringConstants { get; set; }

    internal WasmCompileOptions Snapshot()
    {
        if (Builtins is null)
        {
            throw new ArgumentNullException(nameof(Builtins));
        }

        var builtins = new string[Builtins.Count];

        for (var i = 0; i < builtins.Length; i++)
        {
            var builtin = Builtins[i];

            if (!String.Equals(builtin, "js-string", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unsupported WebAssembly builtin set '{builtin}'.", nameof(Builtins));
            }

            builtins[i] = builtin;
        }

        return new WasmCompileOptions
        {
            Builtins = builtins,
            ImportedStringConstants = ImportedStringConstants,
        };
    }

    internal bool HasFeatures => Builtins.Count > 0 || ImportedStringConstants is not null;
}