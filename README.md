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
- Instance export access
    - `exports` lookup by export name
    - export key enumeration
- Export invocation helpers
    - `instance.Invoke(...)`
    - `WasmJsExportedFunction.Invoke(...)`
- Multi-target support for `net8.0` and `net10.0`

## Current Scope and Limitations

AngleSharp.Wasm currently provides a practical subset of the WebAssembly JS API.

- Bridge methods are synchronous from the caller perspective.
- Promise-based namespace operations are not currently exposed.
- `validate(...)` and streaming APIs are not yet implemented.
- Compile options such as builtins / imported string constants are not yet implemented.
- Non-function exports are currently represented as descriptors (`name`, `kind`) rather than full `Memory` / `Table` / `Global` / `Tag` objects.

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
