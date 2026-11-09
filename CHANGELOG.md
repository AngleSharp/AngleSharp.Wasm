# 1.1.1

Released on Monday, November 9 2026.

- Fixed host import arguments arriving as `double` instead of their WebAssembly types (#18) @michaelthyregod
- Added trimming and Native AOT compatibility (`IsAotCompatible`) for the .NET 8 and .NET 10 targets (#17) @michaelthyregod

# 1.1.0

Released on Friday, August 21 2026.

- Updated with compile options support (#13)
- Added `WebAssembly.CompileError`, `WebAssembly.LinkError`, and `WebAssembly.RuntimeError` projections (#14)
- Added `WebAssembly.compileStreaming` and `WebAssembly.instantiateStreaming` methods (#12)
- Added `WebAssembly.validate` method (#11)
- Added `WebAssembly.Exception` object projection (#10)
- Added `WebAssembly.Tag` object projection (#9)
- Added `WebAssembly.Global` object projection (#8)
- Added `WebAssembly.Table` object projection (#7)
- Added `WebAssembly.Memory` object projection (#6)

# 1.0.0

Released on Friday, July 31 2026.

- Added `wasmtime` as Wasm engine
- Added integration with exposure as DOM objects

# 0.13.0

Released on Sunday, June 6 2021.

- Initial release (placeholder)
