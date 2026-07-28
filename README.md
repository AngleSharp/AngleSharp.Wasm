![logo](https://raw.githubusercontent.com/AngleSharp/AngleSharp.Wasm/main/header.png)

# AngleSharp.Wasm

[![CI](https://github.com/AngleSharp/AngleSharp.Wasm/actions/workflows/ci.yml/badge.svg)](https://github.com/AngleSharp/AngleSharp.Wasm/actions/workflows/ci.yml)
[![GitHub Tag](https://img.shields.io/github/tag/AngleSharp/AngleSharp.Wasm.svg?style=flat-square)](https://github.com/AngleSharp/AngleSharp.Wasm/releases)
[![NuGet Count](https://img.shields.io/nuget/dt/AngleSharp.Wasm.svg?style=flat-square)](https://www.nuget.org/packages/AngleSharp.Wasm/)
[![Issues Open](https://img.shields.io/github/issues/AngleSharp/AngleSharp.Wasm.svg?style=flat-square)](https://github.com/AngleSharp/AngleSharp.Wasm/issues)
[![CLA Assistant](https://cla-assistant.io/readme/badge/AngleSharp/AngleSharp.Wasm?style=flat-square)](https://cla-assistant.io/AngleSharp/AngleSharp.Wasm)

AngleSharp.Wasm extends the core AngleSharp library with the ability to run WebAssembly. This repository is the home of the source for the AngleSharp.Wasm NuGet package.

## Basic Configuration

If you just want a configuration *that works* you should use the following code:

```cs
var config = Configuration.Default
    .WithWasm(); // from AngleSharp.Wasm
```

This will register everything related for running WebAssembly.

## Features

(tbd)

## Participating

Participation in the project is highly welcome. For this project the same rules as for the AngleSharp core project may be applied.

If you have any question, concern, or spot an issue then please report it before opening a pull request. An initial discussion is appreciated regardless of the nature of the problem.

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.

For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

## License

AngleSharp.Wasm is released using the MIT license. For more information see the [license file](./LICENSE).
