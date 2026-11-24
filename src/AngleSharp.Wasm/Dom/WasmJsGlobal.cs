namespace AngleSharp.Wasm.Dom;

using System;
using AngleSharp.Attributes;
using AngleSharp.Dom;
using Wasmtime;

/// <summary>
/// A global of WebAssembly (WebAssembly.Global).
/// </summary>
[DomName("Global")]
[DomExposed("WebAssembly")]
public sealed class WasmJsGlobal
{
    private readonly IWasmGlobal _global;
    private readonly WasmJsRealm? _realm;

    internal WasmJsGlobal(IWasmGlobal global)
    {
        _global = global;
    }

    internal WasmJsGlobal(WasmJsRealm realm, WasmtimeGlobal global)
    {
        _global = global;
        _realm = realm;
    }

    /// <summary>
    /// Creates a global (new WebAssembly.Global).
    /// </summary>
    /// <param name="window">The host.</param>
    /// <param name="descriptor">The type of the value and if it may change.</param>
    /// <param name="value">The value, if any; else the default of the type.</param>
    [DomConstructor]
    public WasmJsGlobal(IWindow window, WasmJsGlobalDescriptor descriptor, object? value = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _realm = WebAssembly.GetRealm(window);
        var kind = ParseKind(descriptor.Value);
        var initial = value is null || value == System.Reflection.Missing.Value ? DefaultOf(kind) : _realm.ToWasm(value, kind);
        var global = new Global(_realm.Store, kind, initial, descriptor.Mutable ? Mutability.Mutable : Mutability.Immutable);
        _global = new WasmtimeGlobal(_realm.Context, global);
    }

    internal Global? Native => (_global as WasmtimeGlobal)?.Native;

    /// <summary>
    /// Gets or sets the value: a number, or a BigInt for i64.
    /// </summary>
    [DomName("value")]
    [DomAccessor(Accessors.Getter | Accessors.Setter)]
    public object? Value
    {
        get => _realm is not null && Native is Global global ? _realm.FromWasm(global.GetValue()) : Wrap(_global.GetValue());
        set
        {
            if (_realm is not null && Native is Global global)
            {
                if (global.Mutability != Mutability.Mutable)
                {
                    throw new InvalidOperationException("The WebAssembly global is immutable.");
                }

                global.SetValue(_realm.ToWasm(value, global.Kind));
            }
            else
            {
                _global.SetValue(Unwrap(value));
            }
        }
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <returns>The value.</returns>
    [DomName("valueOf")]
    public object? ValueOf() => Value;

    internal static ValueKind ParseKind(string? type) => type switch
    {
        "i32" => ValueKind.Int32,
        "i64" => ValueKind.Int64,
        "f32" => ValueKind.Float32,
        "f64" => ValueKind.Float64,
        "anyfunc" or "funcref" => ValueKind.FuncRef,
        "externref" => ValueKind.ExternRef,
        _ => throw new ArgumentException($"'{type}' is no type of WebAssembly."),
    };

    internal static object? DefaultOf(ValueKind kind) => kind switch
    {
        ValueKind.Int32 => 0,
        ValueKind.Int64 => 0L,
        ValueKind.Float32 => 0f,
        ValueKind.Float64 => 0d,
        ValueKind.FuncRef => Function.Null,
        _ => null,
    };

    private static object? Wrap(object? value) =>
        value is IWasmFunction function ? new WasmJsExportedFunction(function) : value;

    private static object? Unwrap(object? value) =>
        value is WasmJsExportedFunction { Function: not null } function ? function.Function : value;
}
