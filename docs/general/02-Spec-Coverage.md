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
| WebAssembly namespace: `validate(bytes)` | Implemented | Exposed as synchronous `WebAssembly.validate(byte[])`, using native Wasmtime validation or compile-and-dispose fallback for custom runtimes. |
| WebAssembly namespace: streaming APIs | Partial | `compileStreaming(IResponse)` and `instantiateStreaming(IResponse)` synchronously consume successful `application/wasm` AngleSharp responses. Response bodies are buffered for metadata extraction. |
| WebAssembly namespace: compile options | Partial | `WasmCompileOptions` validates and routes `builtins` and `importedStringConstants` through compile, byte instantiate, and streaming entry points. Wasmtime 44 cannot execute these options. |
| `Module.exports(module)` | Partial | Available as instance method `module.exports()` returning descriptor objects. |
| `Module.imports(module)` | Partial | Available as instance method `module.imports()` returning descriptor objects. |
| `Module.customSections(module, name)` | Partial | Available as instance method `module.customSections(name)` returning `byte[][]`. |
| `Instance.exports` | Partial | Available as `instance.exports`, with function, memory, table, global, and tag wrappers plus descriptor values for other export kinds. |
| Exported function invocation | Implemented | Supported via `instance.invoke(...)` and `WasmJsExportedFunction.invoke(...)`. |
| Host import functions | Implemented | Supported via `WithWasmImports(...)` and `WasmImportFunction`. |
| Memory object API (`Memory`) | Partial | Exported memories expose `buffer`, `read(offset, count)`, `write(offset, bytes)`, and `grow(delta)`. `buffer` is a current `byte[]` snapshot rather than a live JavaScript `ArrayBuffer`; memory construction and imports are not yet exposed. |
| Table object API (`Table`) | Partial | Exported `funcref` and `externref` tables expose `length`, `get(index)`, `set(index, value)`, and `grow(delta, value)`. Table construction and imports are not yet exposed. |
| Global object API (`Global`) | Partial | Exported numeric and reference globals expose a mutable or immutable `value` accessor and `valueOf()`. Global construction and imports are not yet exposed. |
| Tag object API (`Tag`) | Partial | Host-created and exported tags expose `type().parameters`, including re-exported imported tags. Wasmtime 44 does not expose native Tag handles or enable exception modules through its .NET API. |
| Exception object API (`Exception`) | Partial | Host-created exceptions validate typed payloads and expose `is(tag)` and `getArg(tag, index)`. Runtime-thrown Wasm exceptions cannot be projected through Wasmtime 44. |
| Error constructors (`CompileError`, `LinkError`, `RuntimeError`) | Implemented | Public DOM-named constructors are available, and compile, instantiate/link, and execution failures are translated at bridge boundaries. |
| JS String builtins set | Partial | The `js-string` builtin set is recognized and forwarded to capable custom runtimes; Wasmtime 44 has no corresponding API. |

## Runtime and Type Support

| Area | Status | Notes |
| --- | --- | --- |
| Runtime backend | Implemented | Uses Wasmtime through the default `WithWasm()` registration. |
| Invocation numeric value types | Implemented | `i32`, `i64`, `f32`, `f64` are supported for import/export invocation paths. |
| Extended/reference value kinds | Partial | Table projections support `funcref` and `externref`; broader reference-type invocation marshaling is not yet exposed. |

## Test-Backed Behavior

The current implementation is validated by runtime and bridge tests in the repository, including:

- compile and instantiate flows
- module validation
- response-based compilation and instantiation
- compile option validation and runtime forwarding
- export invocation
- exported memory read, write, and growth
- exported table access, mutation, and growth
- exported global access and mutation
- exported and re-exported tag type metadata
- host-created exception payload validation and inspection
- compile, link, and runtime error translation
- import descriptor extraction
- export descriptor extraction
- custom section lookup behavior
- host import wiring

## Interpretation Notes

This matrix reflects API shape and behavior in AngleSharp.Wasm, not strict one-to-one Web IDL signatures.
Where the specification defines static namespace or Promise-based operations, AngleSharp.Wasm may expose equivalent functionality through synchronous bridge methods.
