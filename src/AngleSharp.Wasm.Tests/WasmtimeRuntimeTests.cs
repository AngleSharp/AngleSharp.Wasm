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

    [Test]
    public async Task CanCompileInstantiateAndInvokeExport()
    {
        using var runtime = new WasmtimeWasmRuntime();
        using var module = await runtime.CompileAsync(MinimalModule).ConfigureAwait(false);
        using var instance = await runtime.InstantiateAsync(module).ConfigureAwait(false);

        var result = await instance.InvokeAsync("answer").ConfigureAwait(false);

        Assert.That(Convert.ToInt32(result), Is.EqualTo(42));
    }
}
