---
title: "Getting Started"
section: "AngleSharp.Wasm"
---
# Getting Started

## Requirements

AngleSharp.Wasm currently targets modern .NET runtimes and is available for .NET 8 and .NET 10.

You need to have AngleSharp installed already. This could be done via NuGet:

```ps1
Install-Package AngleSharp
```

If you want JavaScript-side access to `window.WebAssembly`, you also need a scripting setup (for example, AngleSharp.Js) that discovers exported DOM members from loaded assemblies.

## Getting AngleSharp.Wasm over NuGet

The simplest way of integrating AngleSharp.Wasm to your project is by using NuGet. You can install AngleSharp.Wasm by opening the package manager console (PM) and typing in the following statement:

```ps1
Install-Package AngleSharp.Wasm
```

You can also use the graphical library package manager ("Manage NuGet Packages for Solution"). Searching for "AngleSharp.Wasm" in the official NuGet online feed will find this library.

## Setting up AngleSharp.Wasm

To use AngleSharp.Wasm you need to add it to your `Configuration` coming from AngleSharp itself.

If you just want a configuration *that works* you should use the following code:

```cs
var config = Configuration.Default
    .WithWasm(); // from AngleSharp.Wasm
```

This will register everything related for running WebAssembly.

## Optional: Register Host Imports

If your Wasm module imports host functions, register import providers:

```cs
var config = Configuration.Default
        .WithWasm()
        .WithWasmImports(context =>
        {
                return new[]
                {
                        new WasmImportFunction(
                                moduleName: "host",
                                functionName: "answer",
                                parameterTypes: Array.Empty<WasmValueType>(),
                                resultTypes: new[] { WasmValueType.Int32 },
                                callback: _ => 41),
                };
        });
```

## What Works Today

- `WithWasm()` registers a default Wasmtime-backed runtime factory.
- `WithWasmImports(...)` can provide host imports during instantiation.
- The DOM surface exposes `WebAssembly.compile(...)` and `WebAssembly.instantiate(...)`.
- `Module` metadata APIs are available:
    - `exports()`
    - `imports()`
    - `customSections(sectionName)`
- `Instance` supports:
    - `exports` object access by name
    - `invoke(exportName, ...args)` helper
- Export descriptors expose spec-style fields (`name`, `kind`).

## Current Limitations

AngleSharp.Wasm currently implements a pragmatic subset of the WebAssembly JS API.

- Synchronous bridge calls are used (`compile` / `instantiate` are not Promise-based APIs).
- `WebAssembly.validate(...)`, streaming APIs, and compile options are not implemented.
- `Instance.exports` function members are wrapper objects requiring `.invoke(...)`.
- Non-function exports are currently represented as descriptors (`name`, `kind`), not full `Memory` / `Table` / `Global` / `Tag` objects.
- `customSections(...)` returns payload bytes (`byte[]`) mapped from custom sections.
- Runtime invocation/import marshaling is currently focused on numeric value kinds (`i32`, `i64`, `f32`, `f64`).

These limitations are intentional for the current release scope and can be evolved incrementally.
