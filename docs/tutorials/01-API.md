---
title: "API Documentation"
section: "AngleSharp.Wasm"
---
# API Documentation

This page summarizes the currently available public API surface.

## Configuration Extensions

Extension methods are available on `IConfiguration` via `AngleSharp.Wasm`:

### `WithWasm()`

Registers the default Wasmtime-backed runtime factory.

```cs
var config = Configuration.Default.WithWasm();
```

### `WithWasm(Func<IBrowsingContext, IWasmRuntimeFactory>)`

Registers a custom runtime factory creator.

### `WithWasmImports(IWasmImportProvider)`

Registers a host import provider.

### `WithWasmImports(Func<IBrowsingContext, IEnumerable<WasmImportFunction>>)`

Registers host imports via delegate.

## DOM-Facing API (`AngleSharp.Wasm.Dom`)

When used with a scripting integration that discovers DOM attributes, the following surface is exposed under `WebAssembly`.

### `WebAssembly.compile(byte[] moduleBytes)`

Compiles bytes and returns a `WasmJsModule`.

### `WebAssembly.validate(byte[] moduleBytes)`

Returns whether the bytes form a valid WebAssembly module without instantiating it.

### `WebAssembly.compileStreaming(IResponse source)`

Synchronously consumes a successful `application/wasm` response and returns a compiled `WasmJsModule`.

### `WebAssembly.instantiateStreaming(IResponse source)`

Synchronously consumes a successful `application/wasm` response and returns a `WasmJsInstantiationResult` containing `module` and `instance`.

### `WebAssembly.instantiate(WasmJsModule module)`

Instantiates a compiled module and returns a `WasmJsInstance`.

## `WasmJsModule`

### `exports()`

Returns `WasmModuleExportDescriptor[]` with:

- `name`
- `kind` (`function`, `table`, `memory`, `global`, `tag`)

### `imports()`

Returns `WasmModuleImportDescriptor[]` with:

- `module`
- `name`
- `kind`

### `customSections(string sectionName)`

Returns all matching custom section payloads as `byte[][]`.

## `WasmJsInstance`

### `exports` (getter)

Returns a `WasmJsExports` object.

### `invoke(string exportName, params object?[] arguments)`

Invokes an exported function by name.

## `WasmJsExports`

### `this[string name]`

Returns an export entry by name:

- `WasmJsExportedFunction` for function exports
- `WasmJsMemory` for memory exports
- `WasmJsTable` for table exports
- `WasmJsGlobal` for global exports
- `WasmJsTag` for tag exports
- `WasmJsExportValue` for other non-function exports
- `null` if no export is found

### `keys()`

Returns all export names.

## `WasmJsExportedFunction`

### `invoke(params object?[] arguments)`

Invokes the wrapped exported function.

## `WasmJsMemory`

Provides `buffer`, `read(offset, count)`, `write(offset, bytes)`, and `grow(delta)`.

## `WasmJsTable`

Provides `length`, `get(index)`, `set(index, value)`, and `grow(delta, value)`. Function references returned by `get(...)` expose `invoke(...)`.

## `WasmJsGlobal`

Provides a `value` accessor and `valueOf()`. Assigning to an immutable global throws `InvalidOperationException`.

## `WasmJsTag`

Can be constructed with an ordered array of WebAssembly value type names. Provides `type()`, whose descriptor exposes the tag's `parameters`.

## `WasmJsException`

Construct with a `WasmJsTag` and matching payload array. Provides `is(tag)` and `getArg(tag, index)` and can be thrown and caught as a .NET exception. Runtime-thrown Wasm exceptions are not available through the default Wasmtime 44 backend.

## `WasmJsExportValue`

Descriptor type for non-function exports.

- `name`
- `kind`

## Notes on Scope

This API is intentionally focused on a practical subset of the full WebAssembly JS API spec. See [Questions](03-Questions.md) for limitations and expected behavior.

For a section-by-section status overview, see [Spec Coverage Matrix](../general/02-Spec-Coverage.md).
