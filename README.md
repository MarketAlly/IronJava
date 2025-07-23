# IronJava

[![CI](https://github.com/MarketAlly/IronJava/actions/workflows/ci.yml/badge.svg)](https://github.com/MarketAlly/IronJava/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/IronJava.svg)](https://www.nuget.org/packages/IronJava/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

IronJava is a native .NET library that parses Java source files and provides a strongly-typed Abstract Syntax Tree (AST) accessible in C#. Built for .NET 9, it supports Java 17 syntax with comprehensive visitor pattern implementation, AST transformations, and JSON serialization.

## Features

- **Full Java 17 Support**: Parse modern Java syntax including records, sealed classes, and pattern matching
- **Strongly-Typed AST**: Over 70+ typed AST node classes representing all Java constructs
- **Visitor Pattern**: Dual visitor interfaces for traversing and transforming the AST
- **AST Transformations**: Built-in transformers for renaming, modifier changes, and node removal
- **LINQ-Style Queries**: Search and filter AST nodes with fluent query syntax
- **JSON Serialization**: Export AST to JSON for interoperability
- **Cross-Platform**: Works on Windows, Linux, and macOS
- **Container-Ready**: Optimized for use in Docker containers and cloud environments

## Requirements

- .NET 9.0 or later
- Cross-platform: Windows, Linux, macOS

## Installation

Install IronJava via NuGet:

```bash
dotnet add package IronJava
```

Or via Package Manager Console:

```powershell
Install-Package IronJava
```

## Quick Start

```csharp
using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Query;

// Parse Java source code
var parser = new JavaParser();
var result = parser.Parse(@"
    public class HelloWorld {
        public static void main(String[] args) {
            System.out.println(""Hello, World!"");
        }
    }
");

if (result.Success)
{
    var ast = result.CompilationUnit;
    
    // Find all method declarations
    var methods = ast.FindAll<MethodDeclaration>();
    
    // Query for the main method
    var mainMethod = ast.Query<MethodDeclaration>()
        .WithName("main")
        .WithModifier(Modifiers.Public | Modifiers.Static)
        .ExecuteFirst();
    
    Console.WriteLine($"Found main method: {mainMethod?.Name}");
}
```

## Core Concepts

### AST Structure

IronJava provides a comprehensive typed AST hierarchy:

```csharp
CompilationUnit
├── PackageDeclaration
├── ImportDeclaration[]
└── TypeDeclaration[]
    ├── ClassDeclaration
    ├── InterfaceDeclaration
    ├── EnumDeclaration
    └── AnnotationDeclaration
```

### Visitor Pattern

Use visitors to traverse and analyze the AST:

```csharp
public class MethodCounter : JavaVisitorBase
{
    public int Count { get; private set; }
    
    public override void VisitMethodDeclaration(MethodDeclaration node)
    {
        Count++;
        base.VisitMethodDeclaration(node);
    }
}

// Usage
var counter = new MethodCounter();
ast.Accept(counter);
Console.WriteLine($"Total methods: {counter.Count}");
```

### AST Transformations

Transform your code programmatically:

```csharp
// Rename all occurrences of a variable
var renamer = new IdentifierRenamer("oldName", "newName");
var transformed = ast.Accept(renamer);

// Add final modifier to all classes
var modifier = ModifierTransformer.AddModifier(Modifiers.Final);
var finalClasses = ast.Accept(modifier);

// Chain multiple transformations
var transformer = new TransformationBuilder()
    .AddModifier(Modifiers.Final)
    .RenameIdentifier("oldVar", "newVar")
    .RemoveNodes(node => node is JavaDoc)
    .Build();
    
var result = transformer.Transform(ast);
```

### LINQ-Style Queries

Search the AST with powerful query expressions:

```csharp
// Find all public static methods
var publicStaticMethods = ast
    .FindAll<MethodDeclaration>()
    .Where(m => m.Modifiers.IsPublic() && m.Modifiers.IsStatic());

// Find all classes implementing Serializable
var serializableClasses = ast
    .QueryClasses()
    .Where(c => c.Interfaces.Any(i => i.Name == "Serializable"))
    .Execute();

// Find getter methods
var getters = ast
    .FindAll<MethodDeclaration>()
    .Where(m => m.IsGetter());
```

### JSON Serialization

Export AST to JSON for external tools:

```csharp
var serializer = new AstJsonSerializer();
string json = serializer.Serialize(ast);

// Output:
// {
//   "nodeType": "CompilationUnit",
//   "types": [{
//     "nodeType": "ClassDeclaration",
//     "name": "HelloWorld",
//     "modifiers": ["public"],
//     ...
//   }]
// }
```

## Advanced Usage

### Error Handling

```csharp
var result = parser.Parse(javaSource);

if (!result.Success)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error at {error.Location}: {error.Message}");
    }
}
```

### AST Comparison

```csharp
var comparer = new AstEqualityComparer(
    ignoreLocation: true,
    ignoreJavaDoc: true
);

bool areEqual = comparer.Equals(ast1, ast2);

// Compute differences
var differ = new AstDiffer();
var diff = differ.ComputeDiff(original, modified);

Console.WriteLine($"Added: {diff.Additions.Count()}");
Console.WriteLine($"Deleted: {diff.Deletions.Count()}");
Console.WriteLine($"Modified: {diff.Modifications.Count()}");
```

### Pattern Matching

```csharp
// Check if a class follows singleton pattern
bool isSingleton = classDecl.IsSingletonClass();

// Find all main methods
var mainMethods = ast
    .FindAll<MethodDeclaration>()
    .Where(m => m.IsMainMethod());
```

## Supported Java Features

- ✅ Classes, Interfaces, Enums, Records
- ✅ Annotations and Annotation Types
- ✅ Generics and Type Parameters
- ✅ Lambda Expressions
- ✅ Method References
- ✅ Switch Expressions
- ✅ Pattern Matching
- ✅ Sealed Classes
- ✅ Text Blocks
- ✅ var/Local Type Inference
- ✅ Modules (Java 9+)

## Performance

IronJava is designed for performance:

- Immutable AST nodes for thread safety
- Efficient visitor pattern implementation
- Minimal allocations during parsing
- Optimized for large codebases

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## License

IronJava is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- Built on [ANTLR4](https://www.antlr.org/) with the official Java grammar
- Inspired by [JavaParser](https://javaparser.org/) and [Spoon](https://spoon.gforge.inria.fr/)

## Support

- 📖 [Documentation](https://github.com/MarketAlly/IronJava/wiki)
- 🐛 [Issue Tracker](https://github.com/MarketAlly/IronJava/issues)
- 💬 [Discussions](https://github.com/MarketAlly/IronJava/discussions)
- 📧 Email: dev@marketally.com
