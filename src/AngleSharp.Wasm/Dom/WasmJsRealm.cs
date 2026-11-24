namespace AngleSharp.Wasm.Dom;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using Wasmtime;

/// <summary>
/// The WebAssembly JavaScript API of a browsing context (an agent, in the terms of the
/// standard): its objects (memories, tables, globals, instances) live in one store, so they can
/// be imported by any of its modules, and values are converted as the standard says.
/// </summary>
internal sealed class WasmJsRealm
{
    private static readonly ConditionalWeakTable<IBrowsingContext, WasmJsRealm> Realms = new();

    private readonly ConditionalWeakTable<WasmJsCallable, Function> _functions = new();
    private readonly Dictionary<(ulong, IntPtr), WasmJsCallable> _callables = new();
    private Exception? _pending;

    private WasmJsRealm(WasmtimeWasmRuntime runtime)
    {
        Runtime = runtime;
        Context = runtime.CreateStoreContext();
    }

    internal WasmtimeWasmRuntime Runtime { get; }

    internal WasmtimeStoreContext Context { get; }

    internal Store Store => Context.Store;

    /// <summary>
    /// Gets the realm of the window's browsing context.
    /// </summary>
    public static WasmJsRealm Of(IWindow window, IWasmRuntime runtime)
    {
        var context = window?.Document?.Context ?? throw new InvalidOperationException("The window has no browsing context.");

        return Realms.GetValue(context, _ => runtime is WasmtimeWasmRuntime wasmtime
            ? new WasmJsRealm(wasmtime)
            : throw new NotSupportedException("The WebAssembly JavaScript API needs the Wasmtime runtime (WithWasm())."));
    }

    #region Instantiation

    public WasmJsInstance Instantiate(WasmJsModule module, IDictionary<string, IDictionary<string, WasmJsImportValue>>? importObject)
    {
        var compiled = module.Module as WasmtimeCompiledModule
            ?? throw new ArgumentException("The module was compiled by another runtime.", nameof(module));
        var native = compiled.Module;

        if (native.Imports.Count > 0 && importObject is null)
        {
            throw new ArgumentException("The module has imports, but no import object was given.");
        }

        using var linker = new Linker(Runtime.Engine) { AllowShadowing = true };

        foreach (var import in native.Imports)
        {
            if (importObject is null || !importObject.TryGetValue(import.ModuleName, out var entries) || entries is null)
            {
                throw new ArgumentException($"The import object has no module '{import.ModuleName}'.");
            }

            entries.TryGetValue(import.Name, out var value);
            var label = $"{import.ModuleName}.{import.Name}";

            switch (import)
            {
                case FunctionImport function:
                    if (value?.Function is not WasmJsCallable callable)
                    {
                        throw new WasmJsLinkError($"The import {label} has to be a function.");
                    }

                    linker.Define(import.ModuleName, import.Name, _functions.TryGetValue(callable, out var exported)
                        ? exported
                        : CreateHostFunction(callable, function.Parameters, function.Results));
                    break;
                case GlobalImport global:
                    linker.Define(import.ModuleName, import.Name, GetImportedGlobal(value, global, label));
                    break;
                case MemoryImport:
                    linker.Define(import.ModuleName, import.Name, value?.Value is WasmJsMemory { Native: { } memory }
                        ? memory
                        : throw new WasmJsLinkError($"The import {label} has to be a WebAssembly.Memory."));
                    break;
                case TableImport:
                    linker.Define(import.ModuleName, import.Name, value?.Value is WasmJsTable { Native: { } table }
                        ? table
                        : throw new WasmJsLinkError($"The import {label} has to be a WebAssembly.Table."));
                    break;
            }
        }

        var instance = Run(() => linker.Instantiate(Store, native), error => new WasmJsLinkError(error.Message, error));
        return new WasmJsInstance(this, new WasmtimeInstance(Context, instance), module);
    }

    private Global GetImportedGlobal(WasmJsImportValue? value, GlobalImport import, string label)
    {
        if (value?.Value is WasmJsGlobal { Native: { } global })
        {
            return global;
        }

        if (value is null || value.Value is null || import.Mutability == Mutability.Mutable)
        {
            throw new WasmJsLinkError($"The import {label} has to be a WebAssembly.Global or a number.");
        }

        if (import.Kind == ValueKind.Int64 ? value.Value is not BigInteger : value.Value is BigInteger)
        {
            throw new WasmJsLinkError($"The import {label} has to be a {(import.Kind == ValueKind.Int64 ? "BigInt" : "number")}.");
        }

        return new Global(Store, import.Kind, ToWasm(value.Value, import.Kind), Mutability.Immutable);
    }

    /// <summary>
    /// Gets the exports of an instance as scripts see them: functions, and the objects of the
    /// memories, tables and globals, in the order of the module.
    /// </summary>
    public IDictionary<string, object?> GetExports(Instance instance, Wasmtime.Module module)
    {
        var exports = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var export in module.Exports)
        {
            var name = export.Name;
            exports[name] = export switch
            {
                FunctionExport => GetCallable(instance.GetFunction(name)!),
                MemoryExport => new WasmJsMemory(this, new WasmtimeMemory(instance.GetMemory(name)!)),
                TableExport => new WasmJsTable(this, new WasmtimeTable(Context, instance.GetTable(name)!)),
                GlobalExport => new WasmJsGlobal(this, new WasmtimeGlobal(Context, instance.GetGlobal(name)!)),
                _ => null,
            };
        }

        return exports;
    }

    #endregion

    /// <summary>
    /// Creates a memory (new WebAssembly.Memory). Wasmtime creates the memories of instances
    /// only by the host memory creator, so it is the export of a module of just a memory.
    /// </summary>
    public Memory CreateMemory(long minimum, long? maximum)
    {
        if (!Runtime.HasHostMemory)
        {
            return new Memory(Store, minimum, maximum, false);
        }

        using var module = Wasmtime.Module.FromBytes(Runtime.Engine, "memory", CreateMemoryModule(minimum, maximum));
        var instance = new Instance(Store, module);
        return instance.GetMemory("m")!;
    }

    //  (module (memory (export "m") minimum maximum))
    private static byte[] CreateMemoryModule(long minimum, long? maximum)
    {
        var limits = new List<byte> { maximum is null ? (byte)0 : (byte)1 };
        limits.AddRange(Leb128((ulong)minimum));

        if (maximum is long max)
        {
            limits.AddRange(Leb128((ulong)max));
        }

        var bytes = new List<byte> { 0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00 };
        bytes.Add(0x05);
        bytes.AddRange(Leb128((ulong)(limits.Count + 1)));
        bytes.Add(0x01);
        bytes.AddRange(limits);
        bytes.AddRange(new byte[] { 0x07, 0x05, 0x01, 0x01, (byte)'m', 0x02, 0x00 });
        return bytes.ToArray();
    }

    private static IEnumerable<byte> Leb128(ulong value)
    {
        do
        {
            var current = (byte)(value & 0x7F);
            value >>= 7;
            yield return value != 0 ? (byte)(current | 0x80) : current;
        }
        while (value != 0);
    }

    #region Functions

    /// <summary>
    /// Gets the function scripts call for a function of WebAssembly (an "Exported Function"),
    /// which is the same one for the same function.
    /// </summary>
    public WasmJsCallable GetCallable(Function function)
    {
        var key = GetKey(function);

        if (key is not null && _callables.TryGetValue(key.Value, out var known))
        {
            return known;
        }

        WasmJsCallable callable = arguments => Invoke(function, arguments);
        _functions.Add(callable, function);

        if (key is not null)
        {
            _callables[key.Value] = callable;
        }

        return callable;
    }

    private static readonly FieldInfo? FuncField = typeof(Function).GetField("func", BindingFlags.Instance | BindingFlags.NonPublic);

    //  Wasmtime gives a new object for the same function each time; what it stands for is the
    //  store and index of wasmtime_func_t, which is all the fields of the struct.
    private static (ulong, IntPtr)? GetKey(Function function)
    {
        if (FuncField?.GetValue(function) is { } func)
        {
            var fields = func.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fields.Length == 2 && fields[0].GetValue(func) is ulong store && fields[1].GetValue(func) is IntPtr index)
            {
                return (store, index);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the function of WebAssembly behind a function a script gave, if it is one.
    /// </summary>
    public Function? FindFunction(WasmJsCallable callable) =>
        _functions.TryGetValue(callable, out var function) ? function : null;

    private object? Invoke(Function function, object?[] arguments)
    {
        var parameters = function.Parameters;
        var values = new ValueBox[parameters.Count];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = ToBox(i < arguments.Length ? arguments[i] : Missing.Value, parameters[i]);
        }

        var result = Run(() => function.Invoke(values), error => new WasmJsRuntimeError(error.Message, error));
        var results = function.Results;

        if (results.Count == 0)
        {
            return Missing.Value;
        }
        else if (results.Count == 1)
        {
            return FromWasm(result);
        }

        var list = (object?[])result!;
        var converted = new object?[list.Length];

        for (var i = 0; i < list.Length; i++)
        {
            converted[i] = FromWasm(list[i]);
        }

        return converted;
    }

    private Function CreateHostFunction(WasmJsCallable callable, IReadOnlyList<ValueKind> parameters, IReadOnlyList<ValueKind> results) =>
        Function.FromCallback(Store, (Caller caller, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> values) =>
        {
            try
            {
                var given = new object?[arguments.Length];

                for (var i = 0; i < given.Length; i++)
                {
                    given[i] = FromBox(arguments[i], parameters[i]);
                }

                var result = callable(given);

                if (values.Length == 1)
                {
                    values[0] = ToBox(result, results[0]);
                }
                else if (values.Length > 1)
                {
                    var list = result as IList<object?> ?? throw new ArgumentException("A function with several results has to return an array.");

                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = ToBox(i < list.Count ? list[i] : Missing.Value, results[i]);
                    }
                }
            }
            catch (Exception exception)
            {
                //  what the script threw is what reaches the script calling into WebAssembly
                _pending = exception;
                throw;
            }
        }, parameters, results);

    /// <summary>
    /// Runs WebAssembly: a trap is a RuntimeError, unless a function of a script threw, which
    /// goes on as it is (e.g. the exit of a program compiled by Emscripten).
    /// </summary>
    public T Run<T>(Func<T> action, Func<Exception, Exception> translate)
    {
        _pending = null;

        try
        {
            return Context.Run(static run => run(), action);
        }
        catch (WasmtimeException exception)
        {
            var pending = _pending;
            _pending = null;
            throw pending ?? translate(exception);
        }
    }

    #endregion

    #region Values

    /// <summary>
    /// Converts a value of WebAssembly to what scripts see (ToJSValue): i64 is a BigInt.
    /// </summary>
    public object? FromWasm(object? value) => value switch
    {
        int integer => integer,
        long integer => new BigInteger(integer),
        float number => (double)number,
        double number => number,
        Function { IsNull: true } => null,
        Function function => GetCallable(function),
        _ => value,
    };

    private object? FromBox(ValueBox value, ValueKind kind) => kind switch
    {
        ValueKind.Int32 => value.AsInt32(),
        ValueKind.Int64 => new BigInteger(value.AsInt64()),
        ValueKind.Float32 => (double)value.AsSingle(),
        ValueKind.Float64 => value.AsDouble(),
        ValueKind.FuncRef => FromWasm(value.AsFunction(Store)),
        _ => value.As<object>(),
    };

    /// <summary>
    /// Converts what a script gave to a value of WebAssembly (ToWebAssemblyValue).
    /// </summary>
    public object? ToWasm(object? value, ValueKind kind) => kind switch
    {
        ValueKind.Int32 => ToInt32(ToNumber(value)),
        ValueKind.Int64 => ToBigInt64(value),
        ValueKind.Float32 => (float)ToNumber(value),
        ValueKind.Float64 => ToNumber(value),
        ValueKind.FuncRef => ToFunction(value),
        _ => value == Missing.Value ? null : value,
    };

    private ValueBox ToBox(object? value, ValueKind kind) => kind switch
    {
        ValueKind.Int32 => ToInt32(ToNumber(value)),
        ValueKind.Int64 => ToBigInt64(value),
        ValueKind.Float32 => (float)ToNumber(value),
        ValueKind.Float64 => ToNumber(value),
        ValueKind.FuncRef => ToFunction(value),
        _ => ValueBox.AsBox(value == Missing.Value ? null : value),
    };

    private Function ToFunction(object? value) => value switch
    {
        null => Function.Null,
        WasmJsCallable callable when FindFunction(callable) is { } function => function,
        _ => throw new ArgumentException("The value has to be null or an exported function of WebAssembly."),
    };

    /// <summary>
    /// ToNumber of ECMAScript for what a script gave; a BigInt is no number.
    /// </summary>
    public static double ToNumber(object? value) => value switch
    {
        null => 0,
        double number => number,
        int number => number,
        float number => number,
        long number => number,
        bool flag => flag ? 1 : 0,
        string text => ParseNumber(text),
        BigInteger => throw new ArgumentException("A BigInt can't be converted to a number."),
        _ => double.NaN,
    };

    private static double ParseNumber(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.Length == 0)
        {
            return 0;
        }

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            long.TryParse(trimmed.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hex))
        {
            return hex;
        }

        return trimmed switch
        {
            "Infinity" or "+Infinity" => double.PositiveInfinity,
            "-Infinity" => double.NegativeInfinity,
            _ => double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : double.NaN,
        };
    }

    /// <summary>
    /// ToInt32 of ECMAScript: the number modulo 2^32.
    /// </summary>
    public static int ToInt32(double number)
    {
        if (double.IsNaN(number) || double.IsInfinity(number))
        {
            return 0;
        }

        var truncated = Math.Truncate(number);
        var modulo = truncated % 4294967296.0;
        return unchecked((int)(uint)(modulo < 0 ? modulo + 4294967296.0 : modulo));
    }

    /// <summary>
    /// ToBigInt64 of ECMAScript: a BigInt (or what converts to one) modulo 2^64.
    /// </summary>
    public static long ToBigInt64(object? value)
    {
        var integer = value switch
        {
            BigInteger big => big,
            bool flag => flag ? BigInteger.One : BigInteger.Zero,
            string text => text.Trim().Length == 0
                ? BigInteger.Zero
                : BigInteger.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : throw new ArgumentException($"'{text}' can't be converted to a BigInt."),
            _ => throw new ArgumentException("The value has to be a BigInt."),
        };
        return unchecked((long)(ulong)(integer & ulong.MaxValue));
    }

    #endregion
}
