Project Document: IronJava - Native .NET Parser for Java

Goal:
Build a native .NET library (IronJava) that parses Java source files and outputs an AST accessible and walkable in C#. Linux compatible and usable in containers.

Core Requirements

Parser Architecture

Use ANTLR4 with the official Java grammar

Generate C# lexer/parser

Build a typed C# AST layer over ANTLR parse tree

Language Coverage

Support Java 17 syntax (long-term support version)

Cover:

Class, Interface, Enum declarations

Fields, Methods, Constructors

Packages & Imports

Expressions, Statements

Generics, Annotations

Basic JavaDoc comment extraction

Output Format

Strongly typed C# AST nodes (JavaClassDeclaration, JavaMethodCall, etc.)

Optional JSON AST

Tooling Support

API: JavaParser.Parse(string sourceCode)

Visitor pattern: IJavaSyntaxVisitor

Diagnostics interface for errors/warnings

Testing

Unit tests for each grammar rule

Fuzz tests on real-world Java source files

Integration Targets

Publish as NuGet package

Compatible with analyzers, AI tooling, or refactoring tools

Roadmap

Phase 1: ANTLR grammar integration and parser generation

Phase 2: Typed AST classes and mapping layer

Phase 3: JSON serialization, API polish

Phase 4: CI pipeline, public docs, samples


Existing Resources for IronJava
1. Java Grammar for ANTLR4
📦 Repo: antlr/grammars-v4

✅ Stable, widely used.

🔧 C# Code Gen:

bash
Copy
Edit
antlr4 -Dlanguage=CSharp Java9Lexer.g4 Java9Parser.g4
2. JavaParser (Java Library)
📘 Repo: javaparser/javaparser

💡 Contains extensive models for AST and visitors.

🔁 You can replicate the structure in your C# typed layer.

🧠 Licensing: GPL — do not copy code, but use structure as design reference.

3. Spoon (Advanced Java AST Tooling)
📦 Repo: INRIA/spoon

💡 Great for understanding complex AST like annotations and generics.

4. OpenJDK Parser Source
🧬 You can read the com.sun.tools.javac.parser and com.sun.tools.javac.tree packages from OpenJDK for canonical parsing behavior.

🧠 Deep dive only — not directly portable, but useful for accurate AST node definitions.

🛠️ Shared Tools & Utilities
🔄 ANTLR4 C# Target
📦 NuGet: Antlr4.Runtime.Standard

🔧 Use for all grammar-based parsing.

🧪 Testing
✅ Use Test262 Go for Go syntax edge cases.

✅ Use OpenJDK test suite for Java regression cases.

