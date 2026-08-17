![logo](https://raw.githubusercontent.com/AngleSharp/AngleSharp.Wasm/main/header.png)

# AngleSharp.Wasm

[![CI](https://github.com/AngleSharp/AngleSharp.Wasm/actions/workflows/ci.yml/badge.svg)](https://github.com/AngleSharp/AngleSharp.Wasm/actions/workflows/ci.yml)
[![GitHub Tag](https://img.shields.io/github/tag/AngleSharp/AngleSharp.Wasm.svg?style=flat-square)](https://github.com/AngleSharp/AngleSharp.Wasm/releases)
[![NuGet Count](https://img.shields.io/nuget/dt/AngleSharp.Wasm.svg?style=flat-square)](https://www.nuget.org/packages/AngleSharp.Wasm/)
[![Issues Open](https://img.shields.io/github/issues/AngleSharp/AngleSharp.Wasm.svg?style=flat-square)](https://github.com/AngleSharp/AngleSharp.Wasm/issues)
[![CLA Assistant](https://cla-assistant.io/readme/badge/AngleSharp/AngleSharp.Wasm?style=flat-square)](https://cla-assistant.io/AngleSharp/AngleSharp.Wasm)

AngleSharp.Wasm extends the core AngleSharp library with the ability to run WebAssembly. This repository is the home of the source for the AngleSharp.Wasm NuGet package.

## Documentation

Further documentation is available in the `docs` folder:

- [Getting Started](docs/general/01-Basics.md)
- [Spec Coverage Matrix](docs/general/02-Spec-Coverage.md)
- [API Documentation](docs/tutorials/01-API.md)
- [Examples](docs/tutorials/02-Examples.md)
- [Frequently Asked Questions](docs/tutorials/03-Questions.md)

## Basic Configuration

If you just want a configuration *that works* you should use the following code:

```cs
var config = Configuration.Default
    .WithWasm(); // from AngleSharp.Wasm
```

This will register everything related for running WebAssembly.

## Host Imports

If your module imports host functions, register imports via `WithWasmImports(...)`:

```cs
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
```

## DOM / JS Integration

When using a scripting setup that discovers DOM-annotated members (for example, AngleSharp.Js with registered assemblies), AngleSharp.Wasm exposes a WebAssembly surface under `window.WebAssembly`.

Available bridge capabilities include:

- Compile module bytes
- Instantiate compiled modules
- Inspect module imports / exports / custom sections
- Access instance exports and invoke exported functions

Typical flow:

```cs
var module = WebAssembly.Compile(context.Current!, wasmBytes);
var instance = WebAssembly.Instantiate(context.Current!, module);
var result = instance.Invoke("answer");
```

## Runtime Backend

The current default backend uses [Wasmtime](https://github.com/bytecodealliance/wasmtime-dotnet) and targets modern .NET runtimes.

## Target Frameworks

The current package targets `net8.0` and `net10.0`.

## Features

- Runtime registration via `WithWasm()`
- Optional host import registration via `WithWasmImports(...)`
- Default Wasmtime backend
- Module metadata extraction
    - `exports()`
    - `imports()`
    - `customSections(name)`
- Module validation via `validate(bytes)`
- Response-based compilation and instantiation via `compileStreaming(response)` and `instantiateStreaming(response)`
- Compile option routing for `builtins` and `importedStringConstants`
- Instance export access
    - `exports` lookup by export name
    - export key enumeration
- Export invocation helpers
    - `instance.Invoke(...)`
    - `WasmJsExportedFunction.Invoke(...)`
- WebAssembly object projections
    - memory `buffer`, `read`, `write`, and `grow`
    - table `length`, `get`, `set`, and `grow`
    - global `value` and `valueOf`
    - tag `type().parameters`
    - exception `is(tag)` and `getArg(tag, index)`
- Multi-target support for `net8.0` and `net10.0`

## Current Scope and Limitations

AngleSharp.Wasm currently provides a practical subset of the WebAssembly JS API.

- Bridge methods are synchronous from the caller perspective.
- Promise-based namespace operations are not currently exposed.
- Streaming APIs synchronously buffer AngleSharp response streams because module metadata requires the complete binary.
- Compile options are validated and forwarded to capable runtimes. Wasmtime 44 does not expose JavaScript string builtins or imported string constants, so requesting either option with the default backend throws `NotSupportedException`.
- Exported memories, tables, globals, and tags have object projections; other non-function exports remain descriptors (`name`, `kind`).
- Host-created tags and exceptions are supported. Wasmtime 44 does not expose native Tag or Exception handles or enable exception modules through its .NET API, so runtime-thrown Wasm exceptions are not yet projected.

See [Spec Coverage Matrix](docs/general/02-Spec-Coverage.md) for a section-by-section status overview.

## Participating

Participation in the project is highly welcome. For this project the same rules as for the AngleSharp core project may be applied.

If you have any question, concern, or spot an issue then please report it before opening a pull request. An initial discussion is appreciated regardless of the nature of the problem.

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.

For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

## License

AngleSharp.Wasm is released using the MIT license. For more information see the [license file](./LICENSE).
