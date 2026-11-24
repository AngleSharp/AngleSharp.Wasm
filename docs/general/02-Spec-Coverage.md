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
| WebAssembly namespace: `compile(bytes)` | Implemented | `WebAssembly.compile(bytes)` gives a promise of a `WebAssembly.Module`, rejected with a `CompileError` for invalid bytes. The synchronous `WebAssembly.Compile(window, bytes)` remains for C#. |
| WebAssembly namespace: `instantiate(...)` | Implemented | `WebAssembly.instantiate(bytes, importObject)` gives a promise of `{ module, instance }`, `instantiate(module, importObject)` one of the instance. Imports are read from the import object (functions, numbers, BigInts, memories, tables and globals); failures reject with a `LinkError` or `RuntimeError`. |
| WebAssembly namespace: `validate(bytes)` | Implemented | Exposed as synchronous `WebAssembly.validate(byte[])`, using native Wasmtime validation or compile-and-dispose fallback for custom runtimes. |
| WebAssembly namespace: streaming APIs | Implemented | `compileStreaming` and `instantiateStreaming` take a `Response` of `fetch()` (or a promise of one, or an AngleSharp `IResponse`), which has to be successful and of the type `application/wasm`; the body is read completely before compiling. |
| WebAssembly namespace: compile options | Partial | `WasmCompileOptions` validates and routes `builtins` and `importedStringConstants` through compile, byte instantiate, and streaming entry points. Wasmtime 44 cannot execute these options. |
| `Module.exports(module)` | Implemented | `WebAssembly.Module.exports(module)` gives descriptor objects (`name`, `kind`). |
| `Module.imports(module)` | Implemented | `WebAssembly.Module.imports(module)` gives descriptor objects (`module`, `name`, `kind`). |
| `Module.customSections(module, name)` | Implemented | `WebAssembly.Module.customSections(module, name)` gives ArrayBuffers. |
| `Instance.exports` | Implemented | An object with the exports in the order of the module: functions scripts call (the same function for the same export), and `WebAssembly.Memory`, `Table` and `Global` objects. `new WebAssembly.Instance(module, importObject)` instantiates synchronously. |
| Exported function invocation | Implemented | Exported functions are functions of scripts, converting their arguments and results as the standard says (`i64` as BigInt); a trap is a `RuntimeError`, and what a function of a script imported by the module threw reaches the caller as it is. |
| Host import functions | Implemented | Functions of scripts in the import object, and C# functions via `WithWasmImports(...)` and `WasmImportFunction`. |
| Memory object API (`Memory`) | Implemented | `new WebAssembly.Memory({ initial, maximum })`, imports and exports. With the default Wasmtime backend `buffer` is an ArrayBuffer sharing the bytes of the memory (the memories are pinned .NET arrays, via Wasmtime's host memory creator), which is a new one after the memory has grown. Shared memories are not supported. |
| Table object API (`Table`) | Implemented | `new WebAssembly.Table({ element, initial, maximum })`, imports and exports, with `length`, `get(index)`, `set(index, value)` and `grow(delta, value)`. |
| Global object API (`Global`) | Implemented | `new WebAssembly.Global({ value, mutable }, value)`, imports (also of numbers and BigInts) and exports, with `value` and `valueOf()`. |
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
Scripts (with AngleSharp.Js) see the API of the standard: `WebAssembly` is a namespace object holding the interfaces (`WebAssembly.Module`, `Instance`, `Memory`, `Table`, `Global`, `Tag`, `Exception` and the errors), and its operations give promises. The synchronous bridge methods remain for C#.
