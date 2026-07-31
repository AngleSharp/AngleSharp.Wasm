namespace AngleSharp.Wasm;

using System;
using System.Collections.Generic;

/// <summary>
/// Delegate executed for a host import function call.
/// </summary>
/// <param name="arguments">The import invocation arguments.</param>
/// <returns>The return value or array of return values.</returns>
public delegate object? WasmImportCallback(IReadOnlyList<object?> arguments);

/// <summary>
/// Describes a host import function exposed to a WebAssembly module.
/// </summary>
public sealed class WasmImportFunction
{
    /// <summary>
    /// Creates a new import function descriptor.
    /// </summary>
    /// <param name="moduleName">The import module name.</param>
    /// <param name="functionName">The import function name.</param>
    /// <param name="parameterTypes">The parameter types expected by the import.</param>
    /// <param name="resultTypes">The result types returned by the import.</param>
    /// <param name="callback">The callback to execute.</param>
    public WasmImportFunction(
        string moduleName,
        string functionName,
        IReadOnlyList<WasmValueType> parameterTypes,
        IReadOnlyList<WasmValueType> resultTypes,
        WasmImportCallback callback)
    {
        ModuleName = !string.IsNullOrWhiteSpace(moduleName)
            ? moduleName
            : throw new ArgumentException("Module name must be provided.", nameof(moduleName));
        FunctionName = !string.IsNullOrWhiteSpace(functionName)
            ? functionName
            : throw new ArgumentException("Function name must be provided.", nameof(functionName));
        ParameterTypes = parameterTypes ?? throw new ArgumentNullException(nameof(parameterTypes));
        ResultTypes = resultTypes ?? throw new ArgumentNullException(nameof(resultTypes));
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Gets the import module name.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Gets the import function name.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Gets the expected parameter types.
    /// </summary>
    public IReadOnlyList<WasmValueType> ParameterTypes { get; }

    /// <summary>
    /// Gets the expected result types.
    /// </summary>
    public IReadOnlyList<WasmValueType> ResultTypes { get; }

    /// <summary>
    /// Gets the callback that implements the host import.
    /// </summary>
    public WasmImportCallback Callback { get; }
}
