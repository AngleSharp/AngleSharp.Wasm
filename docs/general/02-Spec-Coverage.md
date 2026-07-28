---
title: "Spec Coverage Matrix"
section: "AngleSharp.Wasm"
---
# Spec Coverage Matrix

This page maps the current AngleSharp.Wasm implementation to the WebAssembly JavaScript API specification.

Status values:

- Implemented: Available and intended for normal use.
- Partial: Available in a reduced or adapted form.
- Not yet: Not currently implemented.

## Coverage Table

| Spec Area | Status | Notes |
| --- | --- | --- |
| WebAssembly namespace: `compile(bytes)` | Partial | Exposed as synchronous bridge method `WebAssembly.compile(byte[])`. |
| WebAssembly namespace: `instantiate(...)` | Partial | Exposed as synchronous bridge method `WebAssembly.instantiate(WasmJsModule)`. |
| WebAssembly namespace: `validate(bytes)` | Not yet | Not currently exposed. |
| WebAssembly namespace: streaming APIs | Not yet | No `instantiateStreaming` / `compileStreaming`. |
| WebAssembly namespace: compile options | Not yet | No `builtins` / `importedStringConstants` options support yet. |
| `Module.exports(module)` | Partial | Available as instance method `module.exports()` returning descriptor objects. |
| `Module.imports(module)` | Partial | Available as instance method `module.imports()` returning descriptor objects. |
| `Module.customSections(module, name)` | Partial | Available as instance method `module.customSections(name)` returning `byte[][]`. |
| `Instance.exports` | Partial | Available as `instance.exports`, with function wrappers and descriptor values for non-function exports. |
| Exported function invocation | Implemented | Supported via `instance.invoke(...)` and `WasmJsExportedFunction.invoke(...)`. |
| Host import functions | Implemented | Supported via `WithWasmImports(...)` and `WasmImportFunction`. |
| Memory object API (`Memory`) | Not yet | No full JS API `Memory` object projection yet. |
| Table object API (`Table`) | Not yet | No full JS API `Table` object projection yet. |
| Global object API (`Global`) | Not yet | No full JS API `Global` object projection yet. |
| Tag object API (`Tag`) | Not yet | No full JS API `Tag` object projection yet. |
| Exception object API (`Exception`) | Not yet | No full JS API exception projection yet. |
| Error constructors (`CompileError`, `LinkError`, `RuntimeError`) | Not yet | No dedicated namespace error constructor projection yet. |
| JS String builtins set | Not yet | No compile-option builtin-set wiring yet. |

## Runtime and Type Support

| Area | Status | Notes |
| --- | --- | --- |
| Runtime backend | Implemented | Uses Wasmtime through the default `WithWasm()` registration. |
| Invocation numeric value types | Implemented | `i32`, `i64`, `f32`, `f64` are supported for import/export invocation paths. |
| Extended/reference value kinds | Not yet | Rich reference-type projections are not yet exposed through JS API object wrappers. |

## Test-Backed Behavior

The current implementation is validated by runtime and bridge tests in the repository, including:

- compile and instantiate flows
- export invocation
- import descriptor extraction
- export descriptor extraction
- custom section lookup behavior
- host import wiring

## Interpretation Notes

This matrix reflects API shape and behavior in AngleSharp.Wasm, not strict one-to-one Web IDL signatures.
Where the specification defines static namespace or Promise-based operations, AngleSharp.Wasm may expose equivalent functionality through synchronous bridge methods.
