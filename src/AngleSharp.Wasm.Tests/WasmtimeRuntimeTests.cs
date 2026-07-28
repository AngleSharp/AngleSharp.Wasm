namespace AngleSharp.Wasm.Tests;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public sealed class WasmtimeRuntimeTests
{
    // (module (func (export "answer") (result i32) i32.const 42))
    private static readonly byte[] MinimalModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x60, 0x00, 0x01, 0x7F,
        0x03, 0x02, 0x01, 0x00,
        0x07, 0x0A, 0x01, 0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x00,
        0x0A, 0x06, 0x01, 0x04, 0x00, 0x41, 0x2A, 0x0B,
    };

    // (module (func (export "add") (param i32 i32) (result i32) local.get 0 local.get 1 i32.add))
    private static readonly byte[] AddModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x07, 0x01, 0x60, 0x02, 0x7F, 0x7F, 0x01, 0x7F,
        0x03, 0x02, 0x01, 0x00,
        0x07, 0x07, 0x01, 0x03, 0x61, 0x64, 0x64, 0x00, 0x00,
        0x0A, 0x09, 0x01, 0x07, 0x00, 0x20, 0x00, 0x20, 0x01, 0x6A, 0x0B,
    };

    // (module
    //   (import "host" "answer" (func $answer (result i32)))
    //   (func (export "answer_plus_one") (result i32) call $answer i32.const 1 i32.add))
    private static readonly byte[] HostImportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x60, 0x00, 0x01, 0x7F,
        0x02, 0x0F, 0x01, 0x04, 0x68, 0x6F, 0x73, 0x74, 0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x00,
        0x03, 0x02, 0x01, 0x00,
        0x07, 0x13, 0x01, 0x0F, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x5F, 0x70, 0x6C, 0x75, 0x73, 0x5F, 0x6F, 0x6E, 0x65, 0x00, 0x01,
        0x0A, 0x09, 0x01, 0x07, 0x00, 0x10, 0x00, 0x41, 0x01, 0x6A, 0x0B,
    };

    [Test]
    public async Task CanCompileInstantiateAndInvokeExport()
    {
        using var runtime = new WasmtimeWasmRuntime();
        using var module = await runtime.CompileAsync(MinimalModule).ConfigureAwait(false);
        using var instance = await runtime.InstantiateAsync(module).ConfigureAwait(false);

        var result = await instance.InvokeAsync("answer").ConfigureAwait(false);

        Assert.That(Convert.ToInt32(result), Is.EqualTo(42));
    }

    [Test]
    public async Task CanInvokeExportWithArguments()
    {
        using var runtime = new WasmtimeWasmRuntime();
        using var module = await runtime.CompileAsync(AddModule).ConfigureAwait(false);
        using var instance = await runtime.InstantiateAsync(module).ConfigureAwait(false);

        var result = await instance.InvokeAsync("add", new object?[] { 20, 22 }).ConfigureAwait(false);

        Assert.That(Convert.ToInt32(result), Is.EqualTo(42));
    }

    [Test]
    public async Task CanInstantiateWithHostImport()
    {
        using var runtime = new WasmtimeWasmRuntime();
        using var module = await runtime.CompileAsync(HostImportModule).ConfigureAwait(false);

        var imports = new[]
        {
            new WasmImportFunction(
                "host",
                "answer",
                Array.Empty<WasmValueType>(),
                new[] { WasmValueType.Int32 },
                _ => 41),
        };

        using var instance = await runtime.InstantiateAsync(module, imports).ConfigureAwait(false);
        var result = await instance.InvokeAsync("answer_plus_one").ConfigureAwait(false);

        Assert.That(Convert.ToInt32(result), Is.EqualTo(42));
    }
}
