# PDML

PDML (Portable Data Model Language) is a schema and modeling language with multi-target code generation and Language
Server Protocol (LSP) support.

## Projects

- **PDML**: Command-line interface and compiler for PDML files (`pdml`).
- **PDML.Compiler**: Core lexer, parser, abstract syntax tree (AST), and semantic model.
- **PDML.Generator.Abstractions**: Interfaces and base abstractions for code generators.
- **PDML.Generator.CSharp**: C# code generator for PDML definitions.
- **PDML.Generator.CSharp.Roslyn**: Roslyn incremental source generator for automatic C# code generation.
- **PDML.Generator.TypeScript**: TypeScript code generator for PDML definitions.
- **PDML.Plugin.LSP**: Language Server Protocol (LSP) implementation for PDML.
- **PDML.Plugin.VSCode**: Visual Studio Code extension for PDML syntax highlighting and language features.
- **PDML.Compatibility**: Compatibility types and polyfills for older target frameworks (.NET Standard 2.0).
- **PDML.Npm**: Router-Package for the .NET PDML CLI

## License

This project is licensed under the Apache-2.0 License.
