# PDML

Portable Data Model Language (PDML) is a schema and modeling language designed for defining domain models, data structures, and APIs with multi-target code generation and rich Language Server Protocol (LSP) tooling.

## Table of Contents

- [PDML](#pdml-1)
- [PDML.Compiler](#pdmlcompiler)
- [PDML.Generator.Abstractions](#pdmlgeneratorabstractions)
- [PDML.Generator.CSharp](#pdmlgeneratorcsharp)
- [PDML.Generator.CSharp.Roslyn](#pdmlgeneratorcsharproslyn)
- [PDML.Generator.TypeScript](#pdmlgeneratortypescript)
- [PDML.Plugin.LSP](#pdmlpluginlsp)
- [PDML.Plugin.VSCode](#pdmlpluginvscode)
- [PDML.Compatibility](#pdmlcompatibility)
- [PDML.Cli](#pdmlcli)
- [PDML.Plugin.TSPlugin](#pdmlplugintsplugin)
- [License](#license)

---

## Projects

### PDML

The native command-line compiler executable for PDML files (`pdml`). It provides the primary entry point for compiling `.pdml` schemas, running semantic validations, and executing target code generators from the terminal.

The native binary is distributed via GitHub Releases as standalone platform executables and on NPM as platform-specific packages (`@pdml/compiler-<rid>`). It dynamically loads generator extensions at runtime based on CLI parameters or project configuration.

[^ Back to top ^](#table-of-contents)

---

### PDML.Compiler

The core parsing engine and semantic analyzer for PDML. It contains the lexer, parser, abstract syntax tree (AST) definitions, type system, symbol resolution, and diagnostic reporting pipeline.

Published on NuGet as `PDML.Compiler`, this package serves as the foundational library consumed by all code generators, language servers, and compiler toolchains across the ecosystem.

[^ Back to top ^](#table-of-contents)

---

### PDML.Generator.Abstractions

The shared base abstractions, interfaces, and loader APIs used to build PDML code generators. It defines the core `IGenerator` contracts and context models that bridge the compiler's semantic model with language-specific code generation logic.

Published on NuGet as `PDML.Generator.Abstractions`, this package is intended for developers implementing custom code generators for new target languages and frameworks.

[^ Back to top ^](#table-of-contents)

---

### PDML.Generator.CSharp

The official C# code generator for PDML definitions. It transforms parsed PDML schemas into idiomatic, strongly-typed C# models, records, and type definitions with support for customization and serialization attributes.

Published on NuGet as `PDML.Generator.CSharp` (for .NET toolchains) and on NPM as `@pdml/generator-csharp` (for use with the Node.js CLI wrapper).

[^ Back to top ^](#table-of-contents)

---

### PDML.Generator.CSharp.Roslyn

A Roslyn incremental source generator that brings automated C# code generation directly into .NET project builds. It hooks into MSBuild to compile `.pdml` files in real time as part of `dotnet build` without requiring manual pre-build scripts.

Published on NuGet as `PDML.Generator.CSharp.Roslyn`. Once referenced, any `.pdml` file marked as an `AdditionalFiles` item in your `.csproj` automatically generates corresponding C# source files in your project.

[^ Back to top ^](#table-of-contents)

---

### PDML.Generator.TypeScript

The official TypeScript code generator for PDML schemas. It translates PDML definitions into clean TypeScript interfaces, type aliases, and runtime models tailored for modern JavaScript and TypeScript environments.

Published on NuGet as `PDML.Generator.TypeScript` and on NPM as `@pdml/generator-typescript`.

[^ Back to top ^](#table-of-contents)

---

### PDML.Plugin.LSP

A full Language Server Protocol (LSP) implementation for the PDML language. It delivers comprehensive IDE intelligence, including real-time diagnostics, semantic syntax validation, hover tooltips, go-to-definition, and auto-completion.

The LSP server is bundled directly inside the Visual Studio Code extension and can also be consumed as a standalone server binary by other LSP-compliant editors such as Neovim, Emacs, or Helix.

[^ Back to top ^](#table-of-contents)

---

### PDML.Plugin.VSCode

The official Visual Studio Code extension for PDML development. It provides syntax highlighting, language configuration, file icons, and automatic lifecycle management for the embedded PDML Language Server.

Published on the Visual Studio Marketplace as `pdmllp`. It delivers an out-of-the-box editing experience for `.pdml` files in VS Code and compatible editors.

[^ Back to top ^](#table-of-contents)

---

### PDML.Compatibility

A support library containing polyfills, compatibility types, and attributes (such as `IsExternalInit` and nullable metadata) required to enable modern C# language features when compiling against older target frameworks like .NET Standard 2.0.

Published on NuGet as `PDML.Compatibility`, this package ensures consistent compiler and code generator behavior across diverse .NET runtime environments.

[^ Back to top ^](#table-of-contents)

---

### PDML.Cli

A lightweight Node.js CLI launcher and JavaScript API wrapper (`pdml`). It provides a seamless `npx pdml` or local script workflow by automatically resolving the appropriate native compiler binary for the host OS and architecture.

Published on NPM as `@pdml/cli`. It automatically discovers generator packages installed in `node_modules` (such as `@pdml/generator-typescript` and `@pdml/generator-csharp`) and forwards compilation commands to the underlying native engine.

[^ Back to top ^](#table-of-contents)

---

### PDML.Plugin.TSPlugin

A TypeScript Language Service plugin (`tsserver` plugin) that enables seamless `.pdml` file imports directly within TypeScript projects (`import { MyModel } from "./schema.pdml"`).

Published on NPM as `@pdml/tsplugin`. It virtualizes TypeScript declaration files (`.d.ts`) on the fly in memory and in the background, providing immediate type-checking and autocompletion in IDEs without emitting intermediate build artifacts to disk.

[^ Back to top ^](#table-of-contents)

---

## License

This project is licensed under the Apache-2.0 License.

[^ Back to top ^](#table-of-contents)
