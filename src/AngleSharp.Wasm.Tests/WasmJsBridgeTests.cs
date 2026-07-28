namespace AngleSharp.Wasm.Tests;

using NUnit.Framework;
using AngleSharp.Wasm.Dom;
using System.Threading.Tasks;

[TestFixture]
public sealed class WasmJsBridgeTests
{
    [Test]
    public async Task BridgeCanCompileInstantiateAndInvoke()
    {
        // (module (func (export "answer") (result i32) i32.const 42))
        var moduleBytes = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D,
            0x01, 0x00, 0x00, 0x00,
            0x01, 0x05, 0x01, 0x60, 0x00, 0x01, 0x7F,
            0x03, 0x02, 0x01, 0x00,
            0x07, 0x0A, 0x01, 0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x00,
            0x0A, 0x06, 0x01, 0x04, 0x00, 0x41, 0x2A, 0x0B,
        };

        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, moduleBytes);
        var moduleExports = module.Exports();
        var moduleImports = module.Imports();
        var instance = WebAssembly.Instantiate(context.Current!, module);

        var result = instance.Invoke("answer");
        var answerExport = instance.Exports["answer"] as WasmJsExportedFunction;

        Assert.That(result, Is.EqualTo(42));
        Assert.That(moduleImports, Is.Empty);
        Assert.That(moduleExports, Has.Length.EqualTo(1));
        Assert.That(moduleExports[0].Name, Is.EqualTo("answer"));
        Assert.That(moduleExports[0].Kind, Is.EqualTo("function"));
        Assert.That(answerExport, Is.Not.Null);
        Assert.That(answerExport!.Invoke(), Is.EqualTo(42));
    }
}
