namespace AngleSharp.Wasm.Tests;

using AngleSharp.Wasm.Dom;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// The members of the WebAssembly JavaScript API as a scripting library calls them: promises,
/// functions as <see cref="WasmJsCallable"/>, and memory shared as <see cref="Memory{T}"/>.
/// </summary>
[TestFixture]
public sealed class WasmJsApiTests
{
    private const string Program = """
        (module
          (import "env" "twice" (func $twice (param i32) (result i32)))
          (memory (export "memory") 1)
          (func (export "callTwice") (param i32) (result i32) local.get 0 call $twice)
          (func (export "load") (param i32) (result i32) local.get 0 i32.load8_u)
          (func (export "add64") (param i64 i64) (result i64) local.get 0 local.get 1 i64.add)
          (func (export "nothing")))
        """;

    private static IDictionary<string, IDictionary<string, WasmJsImportValue>> Imports(WasmJsCallable twice) =>
        new Dictionary<string, IDictionary<string, WasmJsImportValue>>
        {
            ["env"] = new Dictionary<string, WasmJsImportValue> { ["twice"] = twice },
        };

    [Test]
    public async Task InstantiateGivesFunctionsCallingImports()
    {
        using var context = BrowsingContext.New(Configuration.Default.WithWasm());
        using var document = await context.OpenNewAsync();
        var bytes = Wasmtime.Module.ConvertText(Program);

        var source = await WebAssembly.InstantiateAsync(context.Current!, bytes, Imports(arguments => 2 * Convert.ToInt32(arguments[0])));
        var exports = source.Instance.ScriptExports;
        var callTwice = (WasmJsCallable)exports["callTwice"]!;
        var add64 = (WasmJsCallable)exports["add64"]!;
        var nothing = (WasmJsCallable)exports["nothing"]!;

        Assert.That(callTwice(new object?[] { 21.0 }), Is.EqualTo(42));
        Assert.That(add64(new object?[] { new BigInteger(long.MaxValue), BigInteger.One }), Is.EqualTo(new BigInteger(long.MinValue)));
        Assert.That(nothing(Array.Empty<object?>()), Is.SameAs(Missing.Value));
        Assert.That(source.Instance.ScriptExports["callTwice"], Is.SameAs(callTwice));
    }

    [Test]
    public async Task MemoryIsSharedWithItsBuffer()
    {
        using var context = BrowsingContext.New(Configuration.Default.WithWasm());
        using var document = await context.OpenNewAsync();
        var bytes = Wasmtime.Module.ConvertText(Program);
        var source = await WebAssembly.InstantiateAsync(context.Current!, bytes, Imports(arguments => arguments[0]));
        var memory = (WasmJsMemory)source.Instance.ScriptExports["memory"]!;
        var load = (WasmJsCallable)source.Instance.ScriptExports["load"]!;

        var buffer = (Memory<byte>)memory.ScriptBuffer;
        buffer.Span[3] = 99;

        Assert.That(buffer.Length, Is.EqualTo(65536));
        Assert.That(load(new object?[] { 3.0 }), Is.EqualTo(99));
        Assert.That(memory.Grow(1), Is.EqualTo(1));

        var grown = (Memory<byte>)memory.ScriptBuffer;

        Assert.That(grown.Length, Is.EqualTo(131072));
        Assert.That(grown.Span[3], Is.EqualTo(99));
        Assert.That(((Memory<byte>)memory.ScriptBuffer).Equals(grown), Is.True);
    }

    [Test]
    public async Task FailuresAreTheErrorsOfTheStandard()
    {
        using var context = BrowsingContext.New(Configuration.Default.WithWasm());
        using var document = await context.OpenNewAsync();
        var bytes = Wasmtime.Module.ConvertText(Program);

        Assert.That(async () => await WebAssembly.CompileAsync(context.Current!, new byte[] { 0, 97, 115, 109, 9 }), Throws.TypeOf<WasmJsCompileError>());
        Assert.That(async () => await WebAssembly.InstantiateAsync(context.Current!, bytes, new Dictionary<string, IDictionary<string, WasmJsImportValue>>
        {
            ["env"] = new Dictionary<string, WasmJsImportValue> { ["twice"] = 5.0 },
        }), Throws.TypeOf<WasmJsLinkError>());
    }

    [Test]
    public async Task ExceptionOfAnImportReachesTheCaller()
    {
        using var context = BrowsingContext.New(Configuration.Default.WithWasm());
        using var document = await context.OpenNewAsync();
        var bytes = Wasmtime.Module.ConvertText(Program);
        var thrown = new InvalidOperationException("exit");
        var source = await WebAssembly.InstantiateAsync(context.Current!, bytes, Imports(arguments => throw thrown));
        var callTwice = (WasmJsCallable)source.Instance.ScriptExports["callTwice"]!;

        Assert.That(() => callTwice(new object?[] { 1.0 }), Throws.Exception.SameAs(thrown));
    }
}
