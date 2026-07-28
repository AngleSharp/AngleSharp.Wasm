---
title: "Examples"
section: "AngleSharp.Wasm"
---
# Example Code

This is a (growing) list of examples for every-day usage of AngleSharp.Wasm.

## Basic Setup

```cs
var config = Configuration.Default.WithWasm();
using var context = BrowsingContext.New(config);
using var document = await context.OpenNewAsync();
```

## Compile and Instantiate

```cs
var module = WebAssembly.Compile(context.Current!, wasmBytes);
var instance = WebAssembly.Instantiate(context.Current!, module);
```

## Invoke an Exported Function

```cs
var result = instance.Invoke("answer");
```

You can also invoke through the export wrapper:

```cs
var exported = (WasmJsExportedFunction?)instance.Exports["answer"];
var result = exported?.Invoke();
```

## Invoke an Exported Function with Arguments

```cs
var add = (WasmJsExportedFunction?)instance.Exports["add"];
var result = add?.Invoke(20, 22); // 42
```

## Read Module Export and Import Metadata

```cs
foreach (var exportEntry in module.Exports())
{
	Console.WriteLine($"Export: {exportEntry.Name} ({exportEntry.Kind})");
}

foreach (var importEntry in module.Imports())
{
	Console.WriteLine($"Import: {importEntry.Module}.{importEntry.Name} ({importEntry.Kind})");
}
```

## Read Custom Sections

```cs
var sections = module.CustomSections("meta");

foreach (var payload in sections)
{
	Console.WriteLine($"Section payload length: {payload.Length}");
}
```

## Register Host Imports

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

With a module importing `host.answer`, an export such as `answer_plus_one` can now be invoked as expected.

## Check for Missing Exports

```cs
var maybe = instance.Exports["does_not_exist"];

if (maybe is null)
{
	Console.WriteLine("Export not found.");
}
```

## List Available Export Names

```cs
var keys = instance.Exports.Keys();
```
