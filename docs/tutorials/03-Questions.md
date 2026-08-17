---
title: "Questions"
section: "AngleSharp.Wasm"
---
# Frequently Asked Questions

For a compact section-by-section status view, see [Spec Coverage Matrix](../general/02-Spec-Coverage.md).

## Which .NET versions are supported?

AngleSharp.Wasm currently targets .NET 8 and .NET 10.

## Is the full WebAssembly JS API specification implemented?

Not yet. The current implementation covers a useful subset with emphasis on:

- Compiling and instantiating modules
- Reading module metadata (`exports`, `imports`, `customSections`)
- Invoking exported functions
- Registering host imports

## Is `window.WebAssembly` available?

Yes, when used with a scripting integration that discovers DOM-annotated members from registered assemblies.

## Are APIs synchronous or asynchronous?

The current bridge methods are synchronous from the caller perspective.

## Are `Memory`, `Table`, `Global`, and `Tag` exposed as JS API objects?

Exported memories, tables, and globals have usable object projections. Host-created and exported tags expose `type().parameters`, including metadata for re-exported imported tags. Host-created exceptions support `is(tag)` and `getArg(tag, index)`. Memory, table, and global construction and imports are not yet exposed.

Wasmtime 44 does not expose native Tag or Exception handles or enable exception modules through its .NET API, so runtime-thrown Wasm exceptions are not yet projected through the default backend.

## How do I invoke exported functions?

Either call:

- `instance.Invoke("exportName", args...)`

or use the export wrapper:

- `((WasmJsExportedFunction)instance.Exports["exportName"]).Invoke(args...)`

## What does `Module.customSections(name)` return?

It returns all matching custom section payloads as `byte[][]`.

## How are host imports provided?

Use `WithWasmImports(...)` with either:

- an `IWasmImportProvider`, or
- a delegate returning `IEnumerable<WasmImportFunction>`.

## Which value kinds are currently supported for invocation/import marshaling?

Current numeric value kinds are supported:

- `i32`
- `i64`
- `f32`
- `f64`

## What are known limitations today?

- No Promise-based `compile` / `instantiate` bridge methods.
- Streaming APIs buffer response bodies and return synchronously rather than returning promises.
- Wasmtime 44 cannot execute the `builtins` or `importedStringConstants` compile options; capable custom runtimes can implement them through `IWasmCompileOptionsCompiler`.
- Runtime-thrown Wasm exception interoperability is not yet available.

These limits are expected at this stage and can be expanded in future versions.
