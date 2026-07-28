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
