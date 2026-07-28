namespace AngleSharp.Wasm.Dom;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

internal sealed class WasmModuleMetadata
{
    private WasmModuleMetadata(
        IReadOnlyList<WasmModuleImportDescriptor> imports,
        IReadOnlyList<WasmModuleExportDescriptor> exports,
        IReadOnlyList<WasmCustomSectionDescriptor> customSections)
    {
        Imports = imports;
        Exports = exports;
        CustomSections = customSections;
    }

    public IReadOnlyList<WasmModuleImportDescriptor> Imports { get; }

    public IReadOnlyList<WasmModuleExportDescriptor> Exports { get; }

    public IReadOnlyList<WasmCustomSectionDescriptor> CustomSections { get; }

    public static WasmModuleMetadata Parse(byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (bytes.Length < 8)
        {
            throw new ArgumentException("The input is too short to be a valid WebAssembly module.", nameof(bytes));
        }

        if (bytes[0] != 0x00 || bytes[1] != 0x61 || bytes[2] != 0x73 || bytes[3] != 0x6D)
        {
            throw new ArgumentException("Missing WebAssembly magic header.", nameof(bytes));
        }

        var imports = new List<WasmModuleImportDescriptor>();
        var exports = new List<WasmModuleExportDescriptor>();
        var customSections = new List<WasmCustomSectionDescriptor>();

        var offset = 8;

        while (offset < bytes.Length)
        {
            var sectionId = bytes[offset++];
            var sectionSize = (int)ReadVarUInt32(bytes, ref offset);
            var sectionEnd = checked(offset + sectionSize);

            if (sectionEnd > bytes.Length)
            {
                throw new ArgumentException("Invalid WebAssembly section size.", nameof(bytes));
            }

            switch (sectionId)
            {
                case 0:
                    ParseCustomSection(bytes, ref offset, sectionEnd, customSections);
                    break;
                case 2:
                    ParseImportSection(bytes, ref offset, sectionEnd, imports);
                    break;
                case 7:
                    ParseExportSection(bytes, ref offset, sectionEnd, exports);
                    break;
                default:
                    offset = sectionEnd;
                    break;
            }
        }

        return new WasmModuleMetadata(imports, exports, customSections);
    }

    private static void ParseImportSection(byte[] bytes, ref int offset, int sectionEnd, List<WasmModuleImportDescriptor> imports)
    {
        var count = ReadVarUInt32(bytes, ref offset);

        for (uint i = 0; i < count; i++)
        {
            var moduleName = ReadName(bytes, ref offset);
            var name = ReadName(bytes, ref offset);
            EnsureRemaining(bytes, offset, sectionEnd, 1);

            var kind = bytes[offset++];
            imports.Add(new WasmModuleImportDescriptor(moduleName, name, MapKind(kind)));
            SkipImportType(bytes, ref offset, sectionEnd, kind);
        }

        offset = sectionEnd;
    }

    private static void ParseExportSection(byte[] bytes, ref int offset, int sectionEnd, List<WasmModuleExportDescriptor> exports)
    {
        var count = ReadVarUInt32(bytes, ref offset);

        for (uint i = 0; i < count; i++)
        {
            var name = ReadName(bytes, ref offset);
            EnsureRemaining(bytes, offset, sectionEnd, 1);
            var kind = bytes[offset++];
            _ = ReadVarUInt32(bytes, ref offset);
            exports.Add(new WasmModuleExportDescriptor(name, MapKind(kind)));
        }

        offset = sectionEnd;
    }

    private static void ParseCustomSection(byte[] bytes, ref int offset, int sectionEnd, List<WasmCustomSectionDescriptor> sections)
    {
        var name = ReadName(bytes, ref offset);
        var remaining = sectionEnd - offset;

        if (remaining < 0)
        {
            throw new ArgumentException("Invalid custom section layout.", nameof(bytes));
        }

        var payload = ArrayPool<byte>.Shared.Rent(remaining);

        try
        {
            Array.Copy(bytes, offset, payload, 0, remaining);
            sections.Add(new WasmCustomSectionDescriptor(name, payload.AsSpan(0, remaining).ToArray()));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(payload);
        }

        offset = sectionEnd;
    }

    private static void SkipImportType(byte[] bytes, ref int offset, int sectionEnd, byte kind)
    {
        switch (kind)
        {
            case 0x00:
                _ = ReadVarUInt32(bytes, ref offset);
                break;
            case 0x01:
                _ = ReadByte(bytes, ref offset, sectionEnd);
                SkipLimits(bytes, ref offset, sectionEnd);
                break;
            case 0x02:
                SkipLimits(bytes, ref offset, sectionEnd);
                break;
            case 0x03:
                _ = ReadByte(bytes, ref offset, sectionEnd);
                _ = ReadByte(bytes, ref offset, sectionEnd);
                break;
            case 0x04:
                _ = ReadByte(bytes, ref offset, sectionEnd);
                _ = ReadVarUInt32(bytes, ref offset);
                break;
            default:
                throw new ArgumentException($"Unsupported WebAssembly import kind '{kind}'.", nameof(bytes));
        }
    }

    private static void SkipLimits(byte[] bytes, ref int offset, int sectionEnd)
    {
        var flags = ReadByte(bytes, ref offset, sectionEnd);
        _ = ReadVarUInt32(bytes, ref offset);

        if ((flags & 0x01) != 0)
        {
            _ = ReadVarUInt32(bytes, ref offset);
        }
    }

    private static string ReadName(byte[] bytes, ref int offset)
    {
        var length = (int)ReadVarUInt32(bytes, ref offset);

        if (length < 0 || offset + length > bytes.Length)
        {
            throw new ArgumentException("Invalid name length in WebAssembly binary.", nameof(bytes));
        }

        var name = Encoding.UTF8.GetString(bytes, offset, length);
        offset += length;
        return name;
    }

    private static byte ReadByte(byte[] bytes, ref int offset, int sectionEnd)
    {
        EnsureRemaining(bytes, offset, sectionEnd, 1);
        return bytes[offset++];
    }

    private static void EnsureRemaining(byte[] bytes, int offset, int sectionEnd, int needed)
    {
        if (offset + needed > sectionEnd || offset + needed > bytes.Length)
        {
            throw new ArgumentException("Unexpected end of WebAssembly section.", nameof(bytes));
        }
    }

    private static uint ReadVarUInt32(byte[] bytes, ref int offset)
    {
        uint result = 0;
        var shift = 0;

        while (true)
        {
            if (offset >= bytes.Length)
            {
                throw new ArgumentException("Unexpected end while reading LEB128 value.", nameof(bytes));
            }

            var current = bytes[offset++];
            result |= (uint)(current & 0x7F) << shift;

            if ((current & 0x80) == 0)
            {
                return result;
            }

            shift += 7;

            if (shift >= 35)
            {
                throw new ArgumentException("Invalid LEB128 value in WebAssembly binary.", nameof(bytes));
            }
        }
    }

    private static string MapKind(byte kind) => kind switch
    {
        0x00 => "function",
        0x01 => "table",
        0x02 => "memory",
        0x03 => "global",
        0x04 => "tag",
        _ => "unknown",
    };
}

/// <summary>
/// Represents a module export descriptor.
/// </summary>
public sealed class WasmModuleExportDescriptor
{
    internal WasmModuleExportDescriptor(string name, string kind)
    {
        Name = name;
        Kind = kind;
    }

    /// <summary>
    /// Gets the exported field name.
    /// </summary>
    [AngleSharp.Attributes.DomName("name")]
    [AngleSharp.Attributes.DomAccessor(AngleSharp.Attributes.Accessors.Getter)]
    public string Name { get; }

    /// <summary>
    /// Gets the exported field kind.
    /// </summary>
    [AngleSharp.Attributes.DomName("kind")]
    [AngleSharp.Attributes.DomAccessor(AngleSharp.Attributes.Accessors.Getter)]
    public string Kind { get; }
}

/// <summary>
/// Represents a module import descriptor.
/// </summary>
public sealed class WasmModuleImportDescriptor
{
    internal WasmModuleImportDescriptor(string module, string name, string kind)
    {
        Module = module;
        Name = name;
        Kind = kind;
    }

    /// <summary>
    /// Gets the import module name.
    /// </summary>
    [AngleSharp.Attributes.DomName("module")]
    [AngleSharp.Attributes.DomAccessor(AngleSharp.Attributes.Accessors.Getter)]
    public string Module { get; }

    /// <summary>
    /// Gets the import field name.
    /// </summary>
    [AngleSharp.Attributes.DomName("name")]
    [AngleSharp.Attributes.DomAccessor(AngleSharp.Attributes.Accessors.Getter)]
    public string Name { get; }

    /// <summary>
    /// Gets the import kind.
    /// </summary>
    [AngleSharp.Attributes.DomName("kind")]
    [AngleSharp.Attributes.DomAccessor(AngleSharp.Attributes.Accessors.Getter)]
    public string Kind { get; }
}

internal sealed class WasmCustomSectionDescriptor
{
    internal WasmCustomSectionDescriptor(string name, byte[] payload)
    {
        Name = name;
        Payload = payload;
    }

    public string Name { get; }

    public byte[] Payload { get; }
}
