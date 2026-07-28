namespace AngleSharp.Wasm.Tests;

using AngleSharp.Wasm.Dom;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

[TestFixture]
public sealed class WasmJsBridgeTests
{
    // (module (func (export "answer") (result i32) i32.const 42))
    private static readonly byte[] AnswerModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x60, 0x00, 0x01, 0x7F,
        0x03, 0x02, 0x01, 0x00,
        0x07, 0x0A, 0x01, 0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x00,
        0x0A, 0x06, 0x01, 0x04, 0x00, 0x41, 0x2A, 0x0B,
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

    // (module (memory (export "mem") 1))
    private static readonly byte[] MemoryExportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x05, 0x03, 0x01, 0x00, 0x01,
        0x07, 0x07, 0x01, 0x03, 0x6D, 0x65, 0x6D, 0x02, 0x00,
    };

    // AnswerModule + two custom sections named "meta"
    private static readonly byte[] AnswerModuleWithCustomSections =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x60, 0x00, 0x01, 0x7F,
        0x03, 0x02, 0x01, 0x00,
        0x07, 0x0A, 0x01, 0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x00,
        0x0A, 0x06, 0x01, 0x04, 0x00, 0x41, 0x2A, 0x0B,
        0x00, 0x08, 0x04, 0x6D, 0x65, 0x74, 0x61, 0x01, 0x02, 0x03,
        0x00, 0x06, 0x04, 0x6D, 0x65, 0x74, 0x61, 0xAA,
    };

    [Test]
    public async Task BridgeCanCompileInstantiateAndInvokeViaInstanceExportsFunction()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, AnswerModule);
        var moduleExports = module.Exports();
        var instance = WebAssembly.Instantiate(context.Current!, module);

        var result = instance.Invoke("answer");
        var answerExport = instance.Exports["answer"] as WasmJsExportedFunction;
        var missingExport = instance.Exports["missing"];
        var exportKeys = instance.Exports.Keys();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(moduleExports, Has.Length.EqualTo(1));
        Assert.That(moduleExports[0].Name, Is.EqualTo("answer"));
        Assert.That(moduleExports[0].Kind, Is.EqualTo("function"));
        Assert.That(answerExport, Is.Not.Null);
        Assert.That(answerExport!.Invoke(), Is.EqualTo(42));
        Assert.That(missingExport, Is.Null);
        Assert.That(exportKeys, Is.EquivalentTo(new[] { "answer" }));
    }

    [Test]
    public async Task ModuleImportsExposeSpecDescriptorFields()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, HostImportModule);
        var imports = module.Imports();

        Assert.That(imports, Has.Length.EqualTo(1));
        Assert.That(imports[0].Module, Is.EqualTo("host"));
        Assert.That(imports[0].Name, Is.EqualTo("answer"));
        Assert.That(imports[0].Kind, Is.EqualTo("function"));
    }

    [Test]
    public async Task ModuleCustomSectionsFilterByNameAndReturnAllMatches()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, AnswerModuleWithCustomSections);
        var sections = module.CustomSections("meta");
        var noSections = module.CustomSections("missing");

        Assert.That(sections, Has.Length.EqualTo(2));
        Assert.That(sections[0], Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
        Assert.That(sections[1], Is.EqualTo(new byte[] { 0xAA }));
        Assert.That(noSections, Is.Empty);
    }

    [Test]
    public async Task InstanceExportsExposeNonFunctionKindsAsValueDescriptors()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, MemoryExportModule);
        var moduleExports = module.Exports();
        var instance = WebAssembly.Instantiate(context.Current!, module);

        var memExport = instance.Exports["mem"] as WasmJsExportValue;

        Assert.That(moduleExports, Has.Length.EqualTo(1));
        Assert.That(moduleExports[0].Name, Is.EqualTo("mem"));
        Assert.That(moduleExports[0].Kind, Is.EqualTo("memory"));
        Assert.That(memExport, Is.Not.Null);
        Assert.That(memExport!.Name, Is.EqualTo("mem"));
        Assert.That(memExport.Kind, Is.EqualTo("memory"));
        Assert.That(instance.Exports.Keys().Single(), Is.EqualTo("mem"));
    }

    [Test]
    public async Task ExportedFunctionWrapperSupportsArguments()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, AddModule);
        var instance = WebAssembly.Instantiate(context.Current!, module);
        var addExport = instance.Exports["add"] as WasmJsExportedFunction;

        Assert.That(addExport, Is.Not.Null);
        Assert.That(addExport!.Invoke(20, 22), Is.EqualTo(42));
    }

    [Test]
    public async Task BridgeCompileThrowsForNullBytes()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        Assert.That(() => WebAssembly.Compile(context.Current!, null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task BridgeInstantiateThrowsForNullModule()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        Assert.That(() => WebAssembly.Instantiate(context.Current!, null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task CustomSectionsThrowsForNullName()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, AnswerModuleWithCustomSections);

        Assert.That(() => module.CustomSections(null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task CustomSectionsLookupIsCaseSensitive()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, AnswerModuleWithCustomSections);

        Assert.That(module.CustomSections("meta"), Has.Length.EqualTo(2));
        Assert.That(module.CustomSections("Meta"), Is.Empty);
    }

    [Test]
    public async Task BridgeInstantiateUsesConfiguredImportProviders()
    {
        var config = Configuration.Default
            .WithWasm()
            .WithWasmImports(_ =>
            {
                return new[]
                {
                    new WasmImportFunction(
                        "host",
                        "answer",
                        Array.Empty<WasmValueType>(),
                        new[] { WasmValueType.Int32 },
                        _ => 41),
                };
            });

        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, HostImportModule);
        var instance = WebAssembly.Instantiate(context.Current!, module);
        var export = instance.Exports["answer_plus_one"] as WasmJsExportedFunction;

        Assert.That(export, Is.Not.Null);
        Assert.That(export!.Invoke(), Is.EqualTo(42));
    }

    [Test]
    public async Task BridgeCompileWithoutWasmRegistrationThrows()
    {
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        Assert.That(() => WebAssembly.Compile(context.Current!, AnswerModule), Throws.InvalidOperationException);
    }
}
