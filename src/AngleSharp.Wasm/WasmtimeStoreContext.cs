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
    /// The state is passed through so callers can use a static lambda and allocate nothing per call.
    /// </summary>
    internal T Run<TState, T>(Func<TState, T> action, TState state)
    {
        var outermost = _depth++ == 0;

        try
        {
            if (outermost && _fuel is ulong fuel)
            {
                Store.Fuel = fuel;
            }

            return action(state);
        }
        finally
        {
            _depth--;
        }
    }
}
