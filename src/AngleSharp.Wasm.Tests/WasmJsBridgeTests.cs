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
        var instance = WebAssembly.Instantiate(context.Current!, module);

        var result = instance.Invoke("answer");

        Assert.That(result, Is.EqualTo(42));
    }
}
