namespace AngleSharp.Wasm.Dom;

using AngleSharp.Attributes;
using System;
using System.Globalization;

/// <summary>
/// JavaScript-visible WebAssembly exception.
/// </summary>
[DomName("Exception")]
public sealed class WasmJsException : Exception
{
    private readonly WasmJsTag _tag;
    private readonly object?[] _payload;

    /// <summary>
    /// Creates a WebAssembly exception with a typed payload.
    /// </summary>
    /// <param name="tag">The exception tag.</param>
    /// <param name="payload">The exception payload.</param>
    public WasmJsException(WasmJsTag tag, object?[] payload)
        : base("wasm exception")
    {
        _tag = tag ?? throw new ArgumentNullException(nameof(tag));

        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        var parameterTypes = tag.Parameters;

        if (payload.Length != parameterTypes.Length)
        {
            throw new ArgumentException(
                $"The payload contains {payload.Length} value(s), but the tag expects {parameterTypes.Length}.",
                nameof(payload));
        }

        _payload = new object?[payload.Length];

        for (var i = 0; i < payload.Length; i++)
        {
            _payload[i] = ConvertValue(payload[i], parameterTypes[i], i);
        }
    }

    /// <summary>
    /// Determines whether this exception was created with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to compare by identity.</param>
    /// <returns>True if the tag matches; otherwise, false.</returns>
    [DomName("is")]
    public bool Is(WasmJsTag tag) => ReferenceEquals(_tag, tag);

    /// <summary>
    /// Gets a payload argument after verifying the tag identity.
    /// </summary>
    /// <param name="tag">The exception tag.</param>
    /// <param name="index">The payload index.</param>
    /// <returns>The payload value.</returns>
    [DomName("getArg")]
    public object? GetArg(WasmJsTag tag, int index)
    {
        if (!Is(tag))
        {
            throw new ArgumentException("The tag does not match this WebAssembly exception.", nameof(tag));
        }

        if ((uint)index >= (uint)_payload.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _payload[index];
    }

    private static object? ConvertValue(object? value, string type, int index)
    {
        try
        {
            return type switch
            {
                "i32" => Convert.ToInt32(value, CultureInfo.InvariantCulture),
                "i64" => Convert.ToInt64(value, CultureInfo.InvariantCulture),
                "f32" => Convert.ToSingle(value, CultureInfo.InvariantCulture),
                "f64" => Convert.ToDouble(value, CultureInfo.InvariantCulture),
                "funcref" when value is null or WasmJsExportedFunction => value,
                "externref" or "anyref" or "eqref" or "i31ref" or "structref" or "arrayref" or "exnref" => value,
                _ => throw new NotSupportedException($"Unsupported WebAssembly exception payload type '{type}'."),
            };
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new ArgumentException(
                $"Payload value at index {index} is not compatible with WebAssembly type '{type}'.",
                nameof(value),
                exception);
        }
    }
}