namespace AngleSharp.Wasm.Tests;

using System;
using System.Threading.Tasks;
using AngleSharp.Wasm.Dom;
using NUnit.Framework;

[TestFixture]
public sealed class WasmtimeRuntimeLimitsTests
{
    // (module
    //   (func (export "spin") (loop br 0))
    //   (func (export "answer") (result i32) i32.const 42))
    private static readonly byte[] SpinModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x08, 0x02, 0x60, 0x00, 0x00, 0x60, 0x00, 0x01, 0x7F,
        0x03, 0x03, 0x02, 0x00, 0x01,
        0x07, 0x11, 0x02, 0x04, 0x73, 0x70, 0x69, 0x6E, 0x00, 0x00, 0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x01,
        0x0A, 0x0E, 0x02, 0x07, 0x00, 0x03, 0x40, 0x0C, 0x00, 0x0B, 0x0B, 0x04, 0x00, 0x41, 0x2A, 0x0B,
    };

    // (module
    //   (import "host" "reenter" (func $reenter))
    //   (func (export "spin") (loop call $reenter br 0))
    //   (func (export "answer") (result i32) i32.const 42))
    private static readonly byte[] ReenteringSpinModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x08, 0x02, 0x60, 0x00, 0x00, 0x60, 0x00, 0x01, 0x7F,
        0x02, 0x10, 0x01, 0x04, 0x68, 0x6F, 0x73, 0x74, 0x07, 0x72, 0x65, 0x65, 0x6E, 0x74, 0x65, 0x72, 0x00, 0x00,
        0x03, 0x03, 0x02, 0x00, 0x01,
        0x07, 0x11, 0x02, 0x04, 0x73, 0x70, 0x69, 0x6E, 0x00, 0x01, 0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x02,
        0x0A, 0x10, 0x02, 0x09, 0x00, 0x03, 0x40, 0x10, 0x00, 0x0C, 0x00, 0x0B, 0x0B, 0x04, 0x00, 0x41, 0x2A, 0x0B,
    };

    // (module (memory (export "mem") 1))
    private static readonly byte[] OnePageMemoryModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x05, 0x03, 0x01, 0x00, 0x01,
        0x07, 0x07, 0x01, 0x03, 0x6D, 0x65, 0x6D, 0x02, 0x00,
    };

    // (module (memory (export "mem") 3))
    private static readonly byte[] ThreePageMemoryModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x05, 0x03, 0x01, 0x00, 0x03,
        0x07, 0x07, 0x01, 0x03, 0x6D, 0x65, 0x6D, 0x02, 0x00,
    };

    // (module (table (export "tab") 1 funcref))
    private static readonly byte[] OneElementTableModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x04, 0x04, 0x01, 0x70, 0x00, 0x01,
        0x07, 0x07, 0x01, 0x03, 0x74, 0x61, 0x62, 0x01, 0x00,
    };

    private const long PageSize = 65536;

    [Test]
    public async Task FuelStopsAnEndlessLoopAndIsRefilledForTheNextCall()
    {
        using var runtime = new WasmtimeWasmRuntime(new WasmRuntimeOptions { Fuel = 10_000 });
        using var module = await runtime.CompileAsync(SpinModule).ConfigureAwait(false);
        using var instance = await runtime.InstantiateAsync(module).ConfigureAwait(false);

        Assert.That(async () => await instance.InvokeAsync("spin").ConfigureAwait(false), Throws.Exception.With.Message.Contains("fuel"));
        Assert.That(async () => await instance.InvokeAsync("spin").ConfigureAwait(false), Throws.Exception.With.Message.Contains("fuel"));
        Assert.That(await instance.InvokeAsync("answer").ConfigureAwait(false), Is.EqualTo(42));
    }

    [Test]
    public async Task FuelAppliesToExportedFunctionsObtainedFromTheInstance()
    {
        using var runtime = new WasmtimeWasmRuntime(new WasmRuntimeOptions { Fuel = 10_000 });
        using var module = await runtime.CompileAsync(SpinModule).ConfigureAwait(false);
        using var instance = await runtime.InstantiateAsync(module).ConfigureAwait(false);
        var functions = (IWasmFunctionProvider)instance;

        Assert.That(() => functions.GetFunction("spin")!.Invoke(), Throws.Exception.With.Message.Contains("fuel"));
        Assert.That(functions.GetFunction("answer")!.Invoke(), Is.EqualTo(42));
    }

    [Test]
    public async Task ReenteringTheInstanceFromAHostImportDoesNotRefillTheFuel()
    {
        using var runtime = new WasmtimeWasmRuntime(new WasmRuntimeOptions { Fuel = 10_000 });
        using var module = await runtime.CompileAsync(ReenteringSpinModule).ConfigureAwait(false);
        IWasmInstance? self = null;
        var reentries = 0;

        var imports = new[]
        {
            new WasmImportFunction(
                "host",
                "reenter",
                Array.Empty<WasmValueType>(),
                Array.Empty<WasmValueType>(),
                _ =>
                {
                    reentries++;
                    return self!.InvokeAsync("answer").AsTask().GetAwaiter().GetResult();
                }),
        };

        using var instance = await runtime.InstantiateAsync(module, imports).ConfigureAwait(false);
        self = instance;

        Assert.That(async () => await instance.InvokeAsync("spin").ConfigureAwait(false), Throws.Exception.With.Message.Contains("fuel"));
        Assert.That(reentries, Is.GreaterThan(0));
        Assert.That(await instance.InvokeAsync("answer").ConfigureAwait(false), Is.EqualTo(42));
    }

    [Test]
    public async Task MaximumMemorySizeStopsGrowthAndLargerInitialMemory()
    {
        using var runtime = new WasmtimeWasmRuntime(new WasmRuntimeOptions { MaximumMemorySize = 2 * PageSize });
        using var module = await runtime.CompileAsync(OnePageMemoryModule).ConfigureAwait(false);
        using var instance = await runtime.InstantiateAsync(module).ConfigureAwait(false);
        var memory = ((IWasmMemoryProvider)instance).GetMemory("mem")!;

        Assert.That(memory.Grow(1), Is.EqualTo(1));
        Assert.That(() => memory.Grow(1), Throws.Exception.With.Message.Contains("failed to grow memory"));
        Assert.That(memory.Length, Is.EqualTo(2 * PageSize));

        using var largerModule = await runtime.CompileAsync(ThreePageMemoryModule).ConfigureAwait(false);
        Assert.That(async () => await runtime.InstantiateAsync(largerModule).ConfigureAwait(false), Throws.Exception.With.Message.Contains("exceeds memory limits"));
    }

    [Test]
    public async Task MaximumTableElementsStopsGrowth()
    {
        using var runtime = new WasmtimeWasmRuntime(new WasmRuntimeOptions { MaximumTableElements = 2 });
        using var module = await runtime.CompileAsync(OneElementTableModule).ConfigureAwait(false);
        using var instance = await runtime.InstantiateAsync(module).ConfigureAwait(false);
        var table = ((IWasmTableProvider)instance).GetTable("tab")!;

        Assert.That(table.Grow(1, null), Is.EqualTo(1UL));
        Assert.That(() => table.Grow(1, null), Throws.Exception.With.Message.Contains("failed to grow table"));
        Assert.That(table.Length, Is.EqualTo(2UL));
    }

    [Test]
    public void NegativeMaximumMemorySizeIsRejected()
    {
        Assert.That(
            () => new WasmtimeWasmRuntime(new WasmRuntimeOptions { MaximumMemorySize = -1 }),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task BridgeReportsSpentFuelAsRuntimeError()
    {
        var config = Configuration.Default.WithWasm(new WasmRuntimeOptions { Fuel = 10_000 });
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var instance = WebAssembly.Instantiate(context.Current!, SpinModule).Instance;

        Assert.That(() => instance.Invoke("spin"), Throws.InstanceOf<WasmJsRuntimeError>().With.Message.Contains("fuel"));
        Assert.That(() => ((WasmJsExportedFunction)instance.Exports["spin"]!).Invoke(), Throws.InstanceOf<WasmJsRuntimeError>().With.Message.Contains("fuel"));
        Assert.That(instance.Invoke("answer"), Is.EqualTo(42));
    }

    [Test]
    public async Task BridgeWithoutOptionsKeepsMemoryUnlimited()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var memory = (WasmJsMemory)WebAssembly.Instantiate(context.Current!, OnePageMemoryModule).Instance.Exports["mem"]!;

        Assert.That(memory.Grow(16), Is.EqualTo(1));
    }
}
