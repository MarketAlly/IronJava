# Contributing to IronJava

Thank you for your interest in contributing to IronJava! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct: be respectful, inclusive, and constructive in all interactions.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/IronJava.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit your changes: `git commit -am 'Add new feature'`
7. Push to your fork: `git push origin feature/your-feature-name`
8. Create a Pull Request

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git

### Building the Project

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Project Structure

```
IronJava/
├── IronJava.Core/          # Main library
│   ├── AST/               # AST node definitions
│   ├── Grammar/           # ANTLR grammar files
│   ├── Parser/            # Parser implementation
│   └── Serialization/     # JSON serialization
├── IronJava.Tests/        # Unit tests
├── IronJava.Benchmarks/   # Performance benchmarks
└── IronJava.Samples/      # Example applications
```

## Coding Standards

### C# Style Guide

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation comments to public APIs
- Keep methods focused and under 50 lines when possible

### Commit Messages

- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit first line to 72 characters
- Reference issues and pull requests when applicable

Example:
```
Add support for Java 17 switch expressions

- Implement new AST nodes for switch expressions
- Add visitor methods for traversal
- Include comprehensive unit tests

Fixes #123
```

## Testing

### Unit Tests

- Write tests for all new functionality
- Maintain or improve code coverage
- Use descriptive test names that explain what is being tested
- Follow AAA pattern: Arrange, Act, Assert

### Integration Tests

- Test real-world Java code parsing
- Verify cross-platform compatibility
- Test error handling and edge cases

## Pull Request Process

1. **Before Submitting**
   - Ensure all tests pass
   - Update documentation if needed
   - Add/update unit tests
   - Run code formatting: `dotnet format`

2. **PR Description**
   - Clearly describe the changes
   - Link related issues
   - Include examples if applicable
   - List any breaking changes

3. **Review Process**
   - Address reviewer feedback promptly
   - Keep discussions focused and professional
   - Be open to suggestions and alternative approaches

## Adding New Features

### AST Nodes

When adding new AST node types:

1. Define the node class in `AST/Nodes/`
2. Implement visitor methods in `IJavaVisitor`
3. Update visitor base classes
4. Add serialization support
5. Create unit tests

### Grammar Changes

If modifying the ANTLR grammar:

1. Update the `.g4` files
2. Regenerate parser/lexer code
3. Update AST builder if needed
4. Test with various Java code samples

## Performance Considerations

- Minimize allocations in hot paths
- Use immutable designs where appropriate
- Profile before optimizing
- Add benchmarks for performance-critical code

## Documentation

- Update README.md for user-facing changes
- Add XML documentation to public APIs
- Include code examples in documentation
- Update wiki for major features

## Questions?

- Open an issue for bugs or feature requests
- Use discussions for questions and ideas
- Join our community chat (if available)

Thank you for contributing to IronJava!