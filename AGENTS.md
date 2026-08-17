# AngleSharp.Wasm

## Purpose

AngleSharp.Wasm extends AngleSharp with WebAssembly execution and a DOM/JavaScript-facing WebAssembly surface. The repository produces the `AngleSharp.Wasm` NuGet package.

## Repository Layout

- `src/AngleSharp.Wasm/`: library implementation.
- `src/AngleSharp.Wasm/Dom/`: DOM-annotated WebAssembly bridge types exposed to scripting integrations.
- `src/AngleSharp.Wasm.Tests/`: NUnit tests for configuration, runtime behavior, and the DOM bridge.
- `src/AngleSharp.Wasm.Docs/`: TypeScript documentation site configuration and entry point.
- `docs/`: Markdown documentation, including the API, examples, and WebAssembly specification coverage.
- `nuke/`: NUKE build project and build configuration.
- `build.sh`, `build.ps1`, `build.cmd`: platform-specific build entry points.

## Development Environment

- The solution is `src/AngleSharp.Wasm.sln`.
- The library and test projects target `net8.0` and `net10.0`.
- The library references AngleSharp 1.x and Wasmtime 44.0.0.
- The default runtime is `WasmtimeWasmRuntime`, registered through `Configuration.WithWasm()`.
- `global.json`, when present, controls the SDK version used by the build wrapper.

## Common Commands

Run from the repository root:

```sh
./build.sh
```

The build wrapper restores local .NET tools and delegates to NUKE. On Windows, use `build.ps1` or `build.cmd`.

For focused local checks, use the .NET CLI against the solution or test project:

```sh
dotnet build src/AngleSharp.Wasm.sln
dotnet test src/AngleSharp.Wasm.Tests/AngleSharp.Wasm.Tests.csproj
```

When changing the DOM bridge or runtime behavior, run the relevant NUnit tests and then the full test project for both target frameworks when practical.

## Architecture and Conventions

- Keep runtime abstractions in `src/AngleSharp.Wasm/` independent of the concrete Wasmtime implementation where possible.
- `IWasmRuntime`, `IWasmCompiledModule`, `IWasmInstance`, and `IWasmRuntimeFactory` define the runtime boundary.
- Host imports are supplied through `IWasmImportProvider`, `WasmImportFunction`, and the `WithWasmImports(...)` configuration extensions.
- The default runtime is Wasmtime-specific; do not make Wasmtime types part of the public abstraction unless the feature requires it.
- DOM-facing members use AngleSharp DOM annotations such as `[DomName]`, `[DomExposed]`, `[DomAccessor]`, and `[DomNoInterfaceObject]`.
- Preserve the existing synchronous bridge style unless deliberately adding a Promise/streaming API.
- Use existing AngleSharp and Wasmtime APIs before introducing new abstractions.
- Keep changes focused and preserve public APIs unless the task explicitly requires a breaking change.
- Use ASCII for new source and documentation unless the surrounding file already requires another character set.

## Current DOM/WebAssembly Surface

The package exposes a minimal `window.WebAssembly` bridge through the types in `src/AngleSharp.Wasm/Dom/`:

- `WebAssembly.compile(byte[])` creates a `WasmJsModule`.
- `WebAssembly.instantiate(WasmJsModule)` creates a `WasmJsInstance`.
- Module metadata includes imports, exports, and custom sections.
- Instance exports support name lookup and function invocation through `WasmJsExportedFunction`.
- Non-function exports are currently descriptor objects rather than full `Memory`, `Table`, `Global`, `Tag`, or `Exception` projections.

The documented gaps include `WebAssembly.validate(...)`, streaming APIs, compile options, full non-function export objects, and dedicated WebAssembly error constructors. Consult `docs/general/02-Spec-Coverage.md` before extending the DOM surface.

## Testing Guidance

Add or update focused NUnit tests in `src/AngleSharp.Wasm.Tests/` for every behavior change. DOM bridge tests should configure a browsing context with `Configuration.Default.WithWasm()`. Runtime tests should cover both successful behavior and invalid input or disposal paths where applicable.

For new WebAssembly binary fixtures, keep the fixture small and document its module shape near the byte array. Prefer testing the public bridge and runtime contracts rather than implementation details of Wasmtime.

## Documentation

Update the relevant Markdown documentation and the specification coverage matrix when adding or changing public WebAssembly functionality. Keep API names aligned with the WebAssembly JavaScript API where the package intentionally provides a compatible projection, and document any synchronous or .NET-specific adaptation.

## Change Hygiene

Do not commit generated output from `bin/`, `obj/`, or NUKE build folders unless the repository explicitly requires it. Do not alter unrelated user changes. Do not commit changes unless explicitly requested.
