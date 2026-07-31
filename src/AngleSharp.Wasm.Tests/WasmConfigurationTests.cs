namespace AngleSharp.Wasm.Tests;

using NUnit.Framework;

[TestFixture]
public sealed class WasmConfigurationTests
{
    [Test]
    public void WithWasmRegistersFactory()
    {
        var config = Configuration.Default.WithWasm();

        Assert.That(config.Has<IWasmRuntimeFactory>(), Is.True);
    }
}
