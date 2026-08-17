namespace AngleSharp.Wasm;

using System;
using System.Globalization;
using Wasmtime;

internal sealed class WasmtimeFunction : IWasmFunction
{
    internal WasmtimeFunction(Function function)
    {
        Function = function;
    }

    internal Function Function { get; }

    public object? Invoke(object?[]? arguments = null)
    {
        var parameters = Function.Parameters;
        var invocationArguments = arguments ?? Array.Empty<object?>();

        if (invocationArguments.Length != parameters.Count)
        {
            throw new ArgumentException($"The function expects {parameters.Count} argument(s), but {invocationArguments.Length} were provided.", nameof(arguments));
        }

        if (invocationArguments.Length == 0)
        {
            return Function.Invoke();
        }

        var boxedArguments = new ValueBox[invocationArguments.Length];

        for (var i = 0; i < boxedArguments.Length; i++)
        {
            boxedArguments[i] = parameters[i] switch
            {
                ValueKind.Int32 => (ValueBox)Convert.ToInt32(invocationArguments[i], CultureInfo.InvariantCulture),
                ValueKind.Int64 => (ValueBox)Convert.ToInt64(invocationArguments[i], CultureInfo.InvariantCulture),
                ValueKind.Float32 => (ValueBox)Convert.ToSingle(invocationArguments[i], CultureInfo.InvariantCulture),
                ValueKind.Float64 => (ValueBox)Convert.ToDouble(invocationArguments[i], CultureInfo.InvariantCulture),
                _ => throw new NotSupportedException($"Unsupported parameter type '{parameters[i]}'."),
            };
        }

        return Function.Invoke(boxedArguments);
    }
}