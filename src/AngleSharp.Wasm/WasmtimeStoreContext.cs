namespace AngleSharp.Wasm;

using System;
using Wasmtime;

/// <summary>
/// Owns a Wasmtime store and refills its fuel when the host calls into it.
/// </summary>
internal sealed class WasmtimeStoreContext
{
    private readonly ulong? _fuel;
    private int _depth;

    internal WasmtimeStoreContext(Store store, ulong? fuel)
    {
        Store = store;
        _fuel = fuel;
    }

    internal Store Store { get; }

    /// <summary>
    /// Runs WebAssembly code in this store. Only the outermost entry refills the fuel, so a host
    /// import that calls back into the store cannot reset the budget of the call it runs in.
    /// </summary>
    internal T Run<T>(Func<T> action)
    {
        var outermost = _depth++ == 0;

        try
        {
            if (outermost && _fuel is ulong fuel)
            {
                Store.Fuel = fuel;
            }

            return action();
        }
        finally
        {
            _depth--;
        }
    }
}
