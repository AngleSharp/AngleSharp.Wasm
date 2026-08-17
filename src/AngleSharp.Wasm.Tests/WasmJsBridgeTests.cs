namespace AngleSharp.Wasm.Tests;

using AngleSharp.Wasm.Dom;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    // (module
    //   (func $answer (export "answer") (result i32) i32.const 42)
    //   (table (export "functions") 1 3 funcref)
    //   (elem (i32.const 0) $answer))
    private static readonly byte[] TableExportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x60, 0x00, 0x01, 0x7F,
        0x03, 0x02, 0x01, 0x00,
        0x04, 0x05, 0x01, 0x70, 0x01, 0x01, 0x03,
        0x07, 0x16, 0x02,
        0x09, 0x66, 0x75, 0x6E, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x73, 0x01, 0x00,
        0x06, 0x61, 0x6E, 0x73, 0x77, 0x65, 0x72, 0x00, 0x00,
        0x09, 0x07, 0x01, 0x00, 0x41, 0x00, 0x0B, 0x01, 0x00,
        0x0A, 0x06, 0x01, 0x04, 0x00, 0x41, 0x2A, 0x0B,
    };

    // (module (table (export "objects") 1 3 externref))
    private static readonly byte[] ExternalReferenceTableExportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x04, 0x05, 0x01, 0x6F, 0x01, 0x01, 0x03,
        0x07, 0x0B, 0x01, 0x07, 0x6F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x73, 0x01, 0x00,
    };

    // (module
    //   (global $counter (export "counter") (mut i32) (i32.const 42))
    //   (global (export "constant") f64 (f64.const 1.5))
    //   (func (export "read_counter") (result i32) global.get $counter))
    private static readonly byte[] GlobalExportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x60, 0x00, 0x01, 0x7F,
        0x03, 0x02, 0x01, 0x00,
        0x06, 0x12, 0x02,
        0x7F, 0x01, 0x41, 0x2A, 0x0B,
        0x7C, 0x00, 0x44, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0x3F, 0x0B,
        0x07, 0x25, 0x03,
        0x07, 0x63, 0x6F, 0x75, 0x6E, 0x74, 0x65, 0x72, 0x03, 0x00,
        0x08, 0x63, 0x6F, 0x6E, 0x73, 0x74, 0x61, 0x6E, 0x74, 0x03, 0x01,
        0x0C, 0x72, 0x65, 0x61, 0x64, 0x5F, 0x63, 0x6F, 0x75, 0x6E, 0x74, 0x65, 0x72, 0x00, 0x00,
        0x0A, 0x06, 0x01, 0x04, 0x00, 0x23, 0x00, 0x0B,
    };

    // (module (global (export "object") (mut externref) (ref.null extern)))
    private static readonly byte[] ExternalReferenceGlobalExportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x06, 0x06, 0x01, 0x6F, 0x01, 0xD0, 0x6F, 0x0B,
        0x07, 0x0A, 0x01, 0x06, 0x6F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x03, 0x00,
    };

    // (module (tag (export "error") (export "alias") (param i32 f64)))
    private static readonly byte[] TagExportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x06, 0x01, 0x60, 0x02, 0x7F, 0x7C, 0x00,
        0x0D, 0x03, 0x01, 0x00, 0x00,
        0x07, 0x11, 0x02,
        0x05, 0x65, 0x72, 0x72, 0x6F, 0x72, 0x04, 0x00,
        0x05, 0x61, 0x6C, 0x69, 0x61, 0x73, 0x04, 0x00,
    };

    // (module
    //   (import "host" "tag" (tag (param i64)))
    //   (export "tag" (tag 0)))
    private static readonly byte[] ImportedTagExportModule =
    {
        0x00, 0x61, 0x73, 0x6D,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x60, 0x01, 0x7E, 0x00,
        0x02, 0x0D, 0x01, 0x04, 0x68, 0x6F, 0x73, 0x74, 0x03, 0x74, 0x61, 0x67, 0x04, 0x00, 0x00,
        0x07, 0x07, 0x01, 0x03, 0x74, 0x61, 0x67, 0x04, 0x00,
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
    public async Task InstanceExportsExposeMemoryAsLiveMemoryObject()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, MemoryExportModule);
        var moduleExports = module.Exports();
        var instance = WebAssembly.Instantiate(context.Current!, module);

        var memExport = instance.Exports["mem"] as WasmJsMemory;

        Assert.That(moduleExports, Has.Length.EqualTo(1));
        Assert.That(moduleExports[0].Name, Is.EqualTo("mem"));
        Assert.That(moduleExports[0].Kind, Is.EqualTo("memory"));
        Assert.That(memExport, Is.Not.Null);
        Assert.That(memExport!.Buffer, Has.Length.EqualTo(65536));

        memExport.Write(42, new byte[] { 1, 2, 3 });

        Assert.That(memExport.Read(42, 3), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(memExport.Buffer[43], Is.EqualTo(2));
        Assert.That(memExport.Grow(1), Is.EqualTo(1));
        Assert.That(memExport.Buffer, Has.Length.EqualTo(2 * 65536));
        Assert.That(instance.Exports.Keys().Single(), Is.EqualTo("mem"));
    }

    [Test]
    public async Task MemoryProjectionValidatesArgumentsAndBounds()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, MemoryExportModule);
        var instance = WebAssembly.Instantiate(context.Current!, module);
        var memory = instance.Exports["mem"] as WasmJsMemory;

        Assert.That(memory, Is.Not.Null);
        Assert.That(() => memory!.Read(-1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => memory!.Read(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => memory!.Read(65536, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => memory!.Write(-1, Array.Empty<byte>()), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => memory!.Write(0, null!), Throws.ArgumentNullException);
        Assert.That(() => memory!.Write(65536, new byte[] { 1 }), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => memory!.Grow(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task InstanceExportsExposeTableAsLiveTableObject()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, TableExportModule);
        var instance = WebAssembly.Instantiate(context.Current!, module);
        var table = instance.Exports["functions"] as WasmJsTable;
        var exportedFunction = instance.Exports["answer"] as WasmJsExportedFunction;

        Assert.That(table, Is.Not.Null);
        Assert.That(table!.Length, Is.EqualTo(1));

        var function = table.Get(0) as WasmJsExportedFunction;

        Assert.That(function, Is.Not.Null);
        Assert.That(function!.Invoke(), Is.EqualTo(42));

        table.Set(0, null);

        Assert.That(table.Get(0), Is.Null);

        table.Set(0, exportedFunction);

        Assert.That(((WasmJsExportedFunction)table.Get(0)!).Invoke(), Is.EqualTo(42));
        Assert.That(table.Grow(1, function), Is.EqualTo(1));
        Assert.That(table.Length, Is.EqualTo(2));
        Assert.That(((WasmJsExportedFunction)table.Get(1)!).Invoke(), Is.EqualTo(42));
        Assert.That(() => table.Get(2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => table.Set(2, null), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task TableProjectionSupportsExternalReferences()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, ExternalReferenceTableExportModule);
        var instance = WebAssembly.Instantiate(context.Current!, module);
        var table = instance.Exports["objects"] as WasmJsTable;
        var value = new object();

        Assert.That(table, Is.Not.Null);
        Assert.That(table!.Get(0), Is.Null);

        table.Set(0, value);

        Assert.That(table.Get(0), Is.SameAs(value));
        Assert.That(table.Grow(1, value), Is.EqualTo(1));
        Assert.That(table.Get(1), Is.SameAs(value));
    }

    [Test]
    public async Task InstanceExportsExposeGlobalAsLiveGlobalObject()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, GlobalExportModule);
        var instance = WebAssembly.Instantiate(context.Current!, module);
        var counter = instance.Exports["counter"] as WasmJsGlobal;
        var constant = instance.Exports["constant"] as WasmJsGlobal;

        Assert.That(counter, Is.Not.Null);
        Assert.That(counter!.Value, Is.EqualTo(42));
        Assert.That(counter.ValueOf(), Is.EqualTo(42));
        Assert.That(constant, Is.Not.Null);
        Assert.That(constant!.Value, Is.EqualTo(1.5));

        counter.Value = 7.0;

        Assert.That(counter.Value, Is.EqualTo(7));
        Assert.That(instance.Invoke("read_counter"), Is.EqualTo(7));
        Assert.That(() => constant.Value = 2.0, Throws.InvalidOperationException);
        Assert.That(constant.Value, Is.EqualTo(1.5));
    }

    [Test]
    public async Task GlobalProjectionSupportsExternalReferences()
    {
        var config = Configuration.Default.WithWasm();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenNewAsync();

        var module = WebAssembly.Compile(context.Current!, ExternalReferenceGlobalExportModule);
        var instance = WebAssembly.Instantiate(context.Current!, module);
        var global = instance.Exports["object"] as WasmJsGlobal;
        var value = new object();

        Assert.That(global, Is.Not.Null);
        Assert.That(global!.Value, Is.Null);

        global.Value = value;

        Assert.That(global.Value, Is.SameAs(value));

        global.Value = null;

        Assert.That(global.Value, Is.Null);
    }

    [Test]
    public void InstanceExportsExposeTagTypeMetadata()
    {
        using var instance = CreateMetadataOnlyInstance(TagExportModule);
        var tag = instance.Exports["error"] as WasmJsTag;
        var alias = instance.Exports["alias"] as WasmJsTag;

        Assert.That(tag, Is.Not.Null);
        Assert.That(tag!.Type().Parameters, Is.EqualTo(new[] { "i32", "f64" }));
        Assert.That(alias, Is.SameAs(tag));

        var parameters = tag.Type().Parameters;
        parameters[0] = "changed";

        Assert.That(tag.Type().Parameters, Is.EqualTo(new[] { "i32", "f64" }));
    }

    [Test]
    public void ReExportedImportedTagRetainsTypeMetadata()
    {
        using var instance = CreateMetadataOnlyInstance(ImportedTagExportModule);
        var tag = instance.Exports["tag"] as WasmJsTag;

        Assert.That(tag, Is.Not.Null);
        Assert.That(tag!.Type().Parameters, Is.EqualTo(new[] { "i64" }));
    }

    [Test]
    public void ExceptionProjectionValidatesAndExposesTypedPayload()
    {
        var tag = new WasmJsTag(new[] { "i32", "f64" });
        var otherTag = new WasmJsTag(new[] { "i32", "f64" });
        var payload = new object?[] { 42.0, 1.5 };
        var exception = new WasmJsException(tag, payload);

        payload[0] = 0;

        Assert.That(exception.Message, Is.EqualTo("wasm exception"));
        Assert.That(exception.Is(tag), Is.True);
        Assert.That(exception.Is(otherTag), Is.False);
        Assert.That(exception.GetArg(tag, 0), Is.EqualTo(42));
        Assert.That(exception.GetArg(tag, 1), Is.EqualTo(1.5));
        Assert.That(() => exception.GetArg(otherTag, 0), Throws.ArgumentException);
        Assert.That(() => exception.GetArg(tag, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => exception.GetArg(tag, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new WasmJsException(tag, new object?[] { 42 }), Throws.ArgumentException);
        Assert.That(() => new WasmJsException(tag, new object?[] { "invalid", 1.5 }), Throws.ArgumentException);
        Assert.That(() => new WasmJsTag(new[] { "invalid" }), Throws.ArgumentException);
    }

    [Test]
    public void ExceptionProjectionCanBeThrownAndCaught()
    {
        var tag = new WasmJsTag(new[] { "i64" });
        var exception = new WasmJsException(tag, new object?[] { 42L });

        var caught = Assert.Throws<WasmJsException>(() => throw exception);

        Assert.That(caught, Is.SameAs(exception));
        Assert.That(caught!.GetArg(tag, 0), Is.EqualTo(42L));
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

    private static MetadataOnlyInstanceScope CreateMetadataOnlyInstance(byte[] moduleBytes)
    {
        var metadataType = typeof(WasmJsModule).Assembly.GetType("AngleSharp.Wasm.Dom.WasmModuleMetadata")!;
        var parse = metadataType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)!;
        var metadata = parse.Invoke(null, new object[] { moduleBytes })!;
        var exports = (IReadOnlyList<WasmModuleExportDescriptor>)metadataType
            .GetProperty("Exports", BindingFlags.Public | BindingFlags.Instance)!
            .GetValue(metadata)!;
        var runtimeInstance = new MetadataOnlyInstance();
        var instance = (WasmJsInstance)Activator.CreateInstance(
            typeof(WasmJsInstance),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { runtimeInstance, exports },
            null)!;
        return new MetadataOnlyInstanceScope(runtimeInstance, instance);
    }

    private sealed class MetadataOnlyInstanceScope : IDisposable
    {
        private readonly MetadataOnlyInstance _runtimeInstance;

        public MetadataOnlyInstanceScope(MetadataOnlyInstance runtimeInstance, WasmJsInstance instance)
        {
            _runtimeInstance = runtimeInstance;
            Exports = instance.Exports;
        }

        public WasmJsExports Exports { get; }

        public void Dispose() => _runtimeInstance.Dispose();
    }

    private sealed class MetadataOnlyInstance : IWasmInstance
    {
        public ValueTask<object?> InvokeAsync(
            string exportName,
            object?[]? arguments = null,
            System.Threading.CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
