# MarketAlly.IronJava.Core API Specification

## Overview

MarketAlly.IronJava.Core is a .NET library that parses Java source code and provides a strongly-typed Abstract Syntax Tree (AST). This specification details all public APIs, types, and usage patterns.

## Table of Contents

1. [Core Parser API](#core-parser-api)
2. [AST Node Types](#ast-node-types)
3. [Visitor Pattern](#visitor-pattern)
4. [AST Query API](#ast-query-api)
5. [AST Transformation API](#ast-transformation-api)
6. [Serialization](#serialization)
7. [Error Handling](#error-handling)

## Core Parser API

### JavaParser Class

**Namespace:** `MarketAlly.IronJava.Core`

```csharp
public class JavaParser
{
    // Parse Java source code and return AST with error information
    public static ParseResult Parse(string sourceCode);
    
    // Parse to ANTLR parse tree (low-level API)
    public static Java9Parser.CompilationUnitContext ParseToAntlrTree(string sourceCode);
}
```

### ParseResult Class

```csharp
public class ParseResult
{
    public CompilationUnit? Ast { get; }
    public List<ParseError> Errors { get; }
    public bool HasErrors { get; }
    
    public ParseResult(CompilationUnit? ast, List<ParseError> errors);
}
```

### ParseError Class

```csharp
public class ParseError
{
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    
    public ParseError(int line, int column, string message);
}
```

## AST Node Types

**Base Namespace:** `MarketAlly.IronJava.Core.AST.Nodes`

### Base Node Types

```csharp
// Base class for all AST nodes
public abstract class JavaNode
{
    public SourceLocation Location { get; set; }
    public abstract void Accept(IJavaVisitor visitor);
    public abstract T Accept<T>(IJavaVisitor<T> visitor);
}

// Location information
public class SourceLocation
{
    public SourcePosition Start { get; set; }
    public SourcePosition End { get; set; }
}

public class SourcePosition
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Position { get; set; }
}
```

### Compilation Unit

```csharp
public class CompilationUnit : JavaNode
{
    public PackageDeclaration? Package { get; set; }
    public List<ImportDeclaration> Imports { get; set; }
    public List<TypeDeclaration> Types { get; set; }
}
```

### Type Declarations

```csharp
public abstract class TypeDeclaration : JavaNode
{
    public string Name { get; set; }
    public Modifiers Modifiers { get; set; }
    public List<Annotation> Annotations { get; set; }
    public List<TypeParameter> TypeParameters { get; set; }
    public JavaDoc? JavaDoc { get; set; }
    
    // Gets the body/members of this type declaration
    public abstract IEnumerable<JavaNode> Body { get; }
}

public class ClassDeclaration : TypeDeclaration
{
    public TypeReference? SuperClass { get; set; }
    public List<TypeReference> Interfaces { get; set; }
    public List<MemberDeclaration> Members { get; set; }
    public List<TypeDeclaration> NestedTypes { get; set; }
    public bool IsRecord { get; set; }
    
    // Body includes both Members and NestedTypes
    public override IEnumerable<JavaNode> Body => Members.Concat(NestedTypes);
}

public class InterfaceDeclaration : TypeDeclaration
{
    public List<TypeReference> ExtendedInterfaces { get; set; }
    public List<MemberDeclaration> Members { get; set; }
    public List<TypeDeclaration> NestedTypes { get; set; }
    
    // Body includes both Members and NestedTypes
    public override IEnumerable<JavaNode> Body => Members.Concat(NestedTypes);
}

public class EnumDeclaration : TypeDeclaration
{
    public List<TypeReference> Interfaces { get; set; }
    public List<EnumConstant> Constants { get; set; }
    public List<MemberDeclaration> Members { get; set; }
    public List<TypeDeclaration> NestedTypes { get; set; }
    
    // Body includes Constants, Members and NestedTypes
    public override IEnumerable<JavaNode> Body => Constants.Concat(Members).Concat(NestedTypes);
}

public class AnnotationDeclaration : TypeDeclaration
{
    public List<AnnotationMember> Members { get; set; }
    public List<TypeDeclaration> NestedTypes { get; set; }
    
    // Body includes both Members and NestedTypes
    public override IEnumerable<JavaNode> Body => Members.Concat(NestedTypes);
}

public class RecordDeclaration : TypeDeclaration
{
    public List<RecordComponent> Components { get; set; }
    public List<TypeNode> Interfaces { get; set; }
    public List<TypeParameter> TypeParameters { get; set; }
}
```

### Member Declarations

```csharp
public abstract class MemberDeclaration : JavaNode
{
    public Modifiers Modifiers { get; set; }
}

public class FieldDeclaration : MemberDeclaration
{
    public TypeNode Type { get; set; }
    public List<VariableDeclarator> Variables { get; set; }
}

public class MethodDeclaration : MemberDeclaration
{
    public string Name { get; set; }
    public TypeNode ReturnType { get; set; }
    public List<Parameter> Parameters { get; set; }
    public List<TypeParameter> TypeParameters { get; set; }
    public List<TypeNode> Throws { get; set; }
    public BlockStatement? Body { get; set; }
}

public class ConstructorDeclaration : MemberDeclaration
{
    public string Name { get; set; }
    public List<Parameter> Parameters { get; set; }
    public List<TypeParameter> TypeParameters { get; set; }
    public List<TypeNode> Throws { get; set; }
    public BlockStatement Body { get; set; }
}

public class InitializerBlock : MemberDeclaration
{
    public bool IsStatic { get; set; }
    public BlockStatement Body { get; set; }
}
```

### Statements

```csharp
public abstract class Statement : JavaNode { }

public class BlockStatement : Statement
{
    public List<Statement> Statements { get; set; }
}

public class ExpressionStatement : Statement
{
    public Expression Expression { get; set; }
}

public class IfStatement : Statement
{
    public Expression Condition { get; set; }
    public Statement ThenStatement { get; set; }
    public Statement? ElseStatement { get; set; }
}

public class WhileStatement : Statement
{
    public Expression Condition { get; set; }
    public Statement Body { get; set; }
}

public class ForStatement : Statement
{
    public List<Statement> Init { get; set; }
    public Expression? Condition { get; set; }
    public List<Expression> Update { get; set; }
    public Statement Body { get; set; }
}

public class EnhancedForStatement : Statement
{
    public VariableDeclaration Variable { get; set; }
    public Expression Iterable { get; set; }
    public Statement Body { get; set; }
}

public class DoWhileStatement : Statement
{
    public Statement Body { get; set; }
    public Expression Condition { get; set; }
}

public class SwitchStatement : Statement
{
    public Expression Expression { get; set; }
    public List<SwitchCase> Cases { get; set; }
}

public class TryStatement : Statement
{
    public List<Resource> Resources { get; set; }
    public BlockStatement Body { get; set; }
    public List<CatchClause> CatchClauses { get; set; }
    public BlockStatement? Finally { get; set; }
}

public class ReturnStatement : Statement
{
    public Expression? Value { get; set; }
}

public class ThrowStatement : Statement
{
    public Expression Expression { get; set; }
}

public class BreakStatement : Statement
{
    public string? Label { get; set; }
}

public class ContinueStatement : Statement
{
    public string? Label { get; set; }
}

public class LabeledStatement : Statement
{
    public string Label { get; set; }
    public Statement Statement { get; set; }
}

public class SynchronizedStatement : Statement
{
    public Expression Lock { get; set; }
    public BlockStatement Body { get; set; }
}

public class AssertStatement : Statement
{
    public Expression Condition { get; set; }
    public Expression? Message { get; set; }
}

public class LocalVariableDeclaration : Statement
{
    public Modifiers Modifiers { get; set; }
    public TypeNode Type { get; set; }
    public List<VariableDeclarator> Variables { get; set; }
}

public class LocalTypeDeclaration : Statement
{
    public TypeDeclaration Declaration { get; set; }
}

public class EmptyStatement : Statement { }

public class YieldStatement : Statement
{
    public Expression Value { get; set; }
}
```

### Expressions

```csharp
public abstract class Expression : JavaNode { }

// Literals
public class IntegerLiteral : Expression
{
    public string Value { get; set; }
}

public class FloatingPointLiteral : Expression
{
    public string Value { get; set; }
}

public class BooleanLiteral : Expression
{
    public bool Value { get; set; }
}

public class CharacterLiteral : Expression
{
    public string Value { get; set; }
}

public class StringLiteral : Expression
{
    public string Value { get; set; }
}

public class TextBlock : Expression
{
    public string Value { get; set; }
}

public class NullLiteral : Expression { }

// Binary expressions
public class BinaryExpression : Expression
{
    public Expression Left { get; set; }
    public BinaryOperator Operator { get; set; }
    public Expression Right { get; set; }
}

public enum BinaryOperator
{
    Add, Subtract, Multiply, Divide, Modulo,
    LeftShift, RightShift, UnsignedRightShift,
    Less, Greater, LessOrEqual, GreaterOrEqual,
    Equal, NotEqual, BitwiseAnd, BitwiseXor, BitwiseOr,
    LogicalAnd, LogicalOr, Instanceof
}

// Unary expressions
public class UnaryExpression : Expression
{
    public UnaryOperator Operator { get; set; }
    public Expression Operand { get; set; }
}

public enum UnaryOperator
{
    Plus, Minus, BitwiseNot, LogicalNot,
    PreIncrement, PreDecrement, PostIncrement, PostDecrement
}

// Other expressions
public class IdentifierExpression : Expression
{
    public string Name { get; set; }
}

public class MemberAccessExpression : Expression
{
    public Expression Object { get; set; }
    public string MemberName { get; set; }
}

public class MethodInvocation : Expression
{
    public Expression? Target { get; set; }
    public string MethodName { get; set; }
    public List<TypeNode> TypeArguments { get; set; }
    public List<Expression> Arguments { get; set; }
}

public class NewExpression : Expression
{
    public TypeNode Type { get; set; }
    public List<Expression> Arguments { get; set; }
    public AnonymousClassBody? AnonymousClassBody { get; set; }
}

public class ArrayCreationExpression : Expression
{
    public TypeNode ElementType { get; set; }
    public List<Expression> Dimensions { get; set; }
    public ArrayInitializer? Initializer { get; set; }
}

public class ArrayAccessExpression : Expression
{
    public Expression Array { get; set; }
    public Expression Index { get; set; }
}

public class CastExpression : Expression
{
    public TypeNode Type { get; set; }
    public Expression Expression { get; set; }
}

public class ConditionalExpression : Expression
{
    public Expression Condition { get; set; }
    public Expression TrueExpression { get; set; }
    public Expression FalseExpression { get; set; }
}

public class AssignmentExpression : Expression
{
    public Expression Left { get; set; }
    public AssignmentOperator Operator { get; set; }
    public Expression Right { get; set; }
}

public enum AssignmentOperator
{
    Assign, AddAssign, SubtractAssign, MultiplyAssign, DivideAssign,
    ModuloAssign, BitwiseAndAssign, BitwiseOrAssign, BitwiseXorAssign,
    LeftShiftAssign, RightShiftAssign, UnsignedRightShiftAssign
}

public class LambdaExpression : Expression
{
    public List<Parameter> Parameters { get; set; }
    public JavaNode Body { get; set; } // Can be Expression or BlockStatement
}

public class MethodReference : Expression
{
    public Expression? Target { get; set; }
    public List<TypeNode> TypeArguments { get; set; }
    public string MethodName { get; set; }
}

public class ThisExpression : Expression
{
    public string? Qualifier { get; set; }
}

public class SuperExpression : Expression
{
    public string? Qualifier { get; set; }
}

public class ParenthesizedExpression : Expression
{
    public Expression Expression { get; set; }
}

public class ClassLiteral : Expression
{
    public TypeNode Type { get; set; }
}

public class SwitchExpression : Expression
{
    public Expression Expression { get; set; }
    public List<SwitchCase> Cases { get; set; }
}

public class PatternExpression : Expression
{
    public Pattern Pattern { get; set; }
}
```

### Types

```csharp
public abstract class TypeNode : JavaNode { }

public class PrimitiveType : TypeNode
{
    public PrimitiveKind Kind { get; set; }
}

public enum PrimitiveKind
{
    Boolean, Byte, Short, Int, Long, Float, Double, Char, Void
}

public class ReferenceType : TypeNode
{
    public string Name { get; set; }
    public List<TypeArgument> TypeArguments { get; set; }
}

public class ArrayType : TypeNode
{
    public TypeNode ElementType { get; set; }
    public int Dimensions { get; set; }
}

public class WildcardType : TypeNode
{
    public WildcardBound? Bound { get; set; }
}

public class WildcardBound
{
    public bool IsUpper { get; set; }
    public TypeNode Type { get; set; }
}

public class IntersectionType : TypeNode
{
    public List<TypeNode> Types { get; set; }
}

public class UnionType : TypeNode
{
    public List<TypeNode> Types { get; set; }
}

public class VarType : TypeNode { }
```

### Annotations

```csharp
public class Annotation : JavaNode
{
    public string Name { get; set; }
    public List<AnnotationArgument> Arguments { get; set; }
}

public class AnnotationArgument : JavaNode
{
    public string? Name { get; set; }
    public Expression Value { get; set; }
}
```

### Modifiers

```csharp
[Flags]
public enum Modifiers
{
    None = 0,
    Public = 1,
    Private = 2,
    Protected = 4,
    Static = 8,
    Final = 16,
    Abstract = 32,
    Native = 64,
    Synchronized = 128,
    Transient = 256,
    Volatile = 512,
    Strictfp = 1024,
    Default = 2048,
    Sealed = 4096,
    NonSealed = 8192
}
```

## Visitor Pattern

**Namespace:** `MarketAlly.IronJava.Core.AST.Visitors`

### Visitor Interfaces

```csharp
// Visitor without return value
public interface IJavaVisitor
{
    void Visit(CompilationUnit node);
    void Visit(PackageDeclaration node);
    void Visit(ImportDeclaration node);
    void Visit(ClassDeclaration node);
    void Visit(InterfaceDeclaration node);
    void Visit(EnumDeclaration node);
    void Visit(AnnotationDeclaration node);
    void Visit(RecordDeclaration node);
    void Visit(FieldDeclaration node);
    void Visit(MethodDeclaration node);
    void Visit(ConstructorDeclaration node);
    void Visit(InitializerBlock node);
    // ... methods for all node types
}

// Visitor with return value
public interface IJavaVisitor<T>
{
    T Visit(CompilationUnit node);
    T Visit(PackageDeclaration node);
    T Visit(ImportDeclaration node);
    T Visit(ClassDeclaration node);
    // ... methods for all node types
}
```

### Base Visitor Implementation

```csharp
public abstract class JavaVisitorBase : IJavaVisitor
{
    // Default implementations that traverse the tree
    public virtual void Visit(CompilationUnit node)
    {
        node.Package?.Accept(this);
        foreach (var import in node.Imports)
            import.Accept(this);
        foreach (var type in node.Types)
            type.Accept(this);
    }
    // ... default implementations for all node types
}

public abstract class JavaVisitorBase<T> : IJavaVisitor<T>
{
    // Generic visitor with return values
    public abstract T DefaultResult { get; }
    public virtual T Aggregate(T result, T nextResult) => nextResult;
    
    public virtual T Visit(CompilationUnit node)
    {
        var result = DefaultResult;
        if (node.Package != null)
            result = Aggregate(result, node.Package.Accept(this));
        // ... aggregate pattern
        return result;
    }
}
```

### Example Visitor Implementations

```csharp
// Count nodes visitor
public class NodeCounterVisitor : JavaVisitorBase<int>
{
    public override int DefaultResult => 0;
    public override int Aggregate(int result, int nextResult) => result + nextResult;
    
    public override int Visit(ClassDeclaration node)
    {
        return 1 + base.Visit(node);
    }
}

// Method collector visitor
public class MethodCollectorVisitor : JavaVisitorBase
{
    public List<MethodDeclaration> Methods { get; } = new();
    
    public override void Visit(MethodDeclaration node)
    {
        Methods.Add(node);
        base.Visit(node);
    }
}
```

## AST Query API

**Namespace:** `MarketAlly.IronJava.Core.AST.Query`

### AstQuery Class

```csharp
public static class AstQuery
{
    // Find all nodes of specific type
    public static IEnumerable<T> FindAll<T>(JavaNode root) where T : JavaNode;
    
    // Find first node of specific type
    public static T? FindFirst<T>(JavaNode root) where T : JavaNode;
    
    // Find nodes matching predicate
    public static IEnumerable<T> Where<T>(JavaNode root, Func<T, bool> predicate) where T : JavaNode;
    
    // Find single node matching predicate
    public static T? FirstOrDefault<T>(JavaNode root, Func<T, bool> predicate) where T : JavaNode;
    
    // Get parent node
    public static JavaNode? GetParent(JavaNode node, JavaNode root);
    
    // Get ancestors
    public static IEnumerable<JavaNode> GetAncestors(JavaNode node, JavaNode root);
    
    // Get descendants
    public static IEnumerable<JavaNode> GetDescendants(JavaNode node);
}
```

### Usage Examples

```csharp
// Find all methods
var methods = AstQuery.FindAll<MethodDeclaration>(compilationUnit);

// Find public classes
var publicClasses = AstQuery.Where<ClassDeclaration>(compilationUnit, 
    c => c.Modifiers.HasFlag(Modifiers.Public));

// Find method by name
var mainMethod = AstQuery.FirstOrDefault<MethodDeclaration>(compilationUnit,
    m => m.Name == "main" && m.Modifiers.HasFlag(Modifiers.Static));

// Get containing class
var containingClass = AstQuery.GetAncestors(method, compilationUnit)
    .OfType<ClassDeclaration>()
    .FirstOrDefault();

// Find all nested types in a class
var outerClass = compilationUnit.Types.OfType<ClassDeclaration>().First();
var allNestedTypes = outerClass.NestedTypes;

// Find nested interfaces specifically
var nestedInterfaces = outerClass.NestedTypes
    .OfType<InterfaceDeclaration>();

// Recursively find all nested types (including deeply nested)
var allNested = AstQuery.FindAll<TypeDeclaration>(outerClass)
    .Where(t => t != outerClass);
```

## AST Transformation API

**Namespace:** `MarketAlly.IronJava.Core.AST.Transformation`

### AstTransformer Class

```csharp
public class AstTransformer
{
    // Clone entire AST
    public static T Clone<T>(T node) where T : JavaNode;
    
    // Transform AST with transformer function
    public static JavaNode Transform(JavaNode node, Func<JavaNode, JavaNode?> transformer);
    
    // Replace nodes matching predicate
    public static JavaNode ReplaceNodes<T>(JavaNode root, Func<T, bool> predicate, Func<T, JavaNode> replacement) where T : JavaNode;
    
    // Remove nodes matching predicate
    public static JavaNode RemoveNodes<T>(JavaNode root, Func<T, bool> predicate) where T : JavaNode;
}
```

### Usage Examples

```csharp
// Clone AST
var clonedAst = AstTransformer.Clone(compilationUnit);

// Rename all methods named "oldName" to "newName"
var transformed = AstTransformer.Transform(compilationUnit, node =>
{
    if (node is MethodDeclaration method && method.Name == "oldName")
    {
        var newMethod = AstTransformer.Clone(method);
        newMethod.Name = "newName";
        return newMethod;
    }
    return node;
});

// Remove all private methods
var withoutPrivateMethods = AstTransformer.RemoveNodes<MethodDeclaration>(
    compilationUnit,
    m => m.Modifiers.HasFlag(Modifiers.Private));

// Add final modifier to all fields
var transformedAst = AstTransformer.ReplaceNodes<FieldDeclaration>(
    compilationUnit,
    f => !f.Modifiers.HasFlag(Modifiers.Final),
    f => {
        var newField = AstTransformer.Clone(f);
        newField.Modifiers |= Modifiers.Final;
        return newField;
    });
```

## Serialization

**Namespace:** `MarketAlly.IronJava.Core.Serialization`

### AstJsonSerializer Class

```csharp
public static class AstJsonSerializer
{
    // Serialize AST to JSON
    public static string Serialize(JavaNode node, bool indented = false);
    
    // Deserialize AST from JSON
    public static T? Deserialize<T>(string json) where T : JavaNode;
    
    // Serialize with custom options
    public static string Serialize(JavaNode node, JsonSerializerOptions options);
    
    // Deserialize with custom options
    public static T? Deserialize<T>(string json, JsonSerializerOptions options) where T : JavaNode;
}
```

### JSON Structure

```json
{
  "nodeType": "CompilationUnit",
  "location": {
    "start": { "line": 1, "column": 0, "position": 0 },
    "end": { "line": 10, "column": 1, "position": 150 }
  },
  "package": null,
  "imports": [],
  "types": [
    {
      "nodeType": "ClassDeclaration",
      "name": "HelloWorld",
      "modifiers": "Public",
      "members": [
        {
          "nodeType": "MethodDeclaration",
          "name": "main",
          "modifiers": "Public, Static",
          "returnType": {
            "nodeType": "PrimitiveType",
            "kind": "Void"
          },
          "parameters": [
            {
              "modifiers": "None",
              "type": {
                "nodeType": "ArrayType",
                "elementType": {
                  "nodeType": "ReferenceType",
                  "name": "String"
                }
              },
              "name": "args"
            }
          ],
          "body": {
            "nodeType": "BlockStatement",
            "statements": []
          }
        }
      ]
    }
  ]
}
```

## Error Handling

### Common Exceptions

```csharp
// Thrown when Java syntax is invalid
public class JavaSyntaxException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public string SourceFile { get; }
}

// Thrown during AST transformation
public class AstTransformationException : Exception
{
    public JavaNode Node { get; }
    public string Operation { get; }
}
```

### Error Recovery

The parser attempts to recover from syntax errors and continue parsing. Check `ParseResult.HasErrors` and `ParseResult.Errors` for syntax errors:

```csharp
var result = JavaParser.Parse(sourceCode);
if (result.HasErrors)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error at {error.Line}:{error.Column}: {error.Message}");
    }
}

// AST may still be partially available even with errors
if (result.Ast != null)
{
    // Process partial AST
}
```

## Complete Example

```csharp
using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Query;
using MarketAlly.IronJava.Core.AST.Transformation;
using MarketAlly.IronJava.Core.AST.Visitors;
using MarketAlly.IronJava.Core.Serialization;

// Parse Java code
var sourceCode = @"
public class Example {
    private String name;
    
    public String getName() {
        return name;
    }
    
    public void setName(String name) {
        this.name = name;
    }
}";

var result = JavaParser.Parse(sourceCode);

if (!result.HasErrors && result.Ast != null)
{
    var ast = result.Ast;
    
    // Query: Find all methods
    var methods = AstQuery.FindAll<MethodDeclaration>(ast);
    Console.WriteLine($"Found {methods.Count()} methods");
    
    // Query: Find getter methods
    var getters = AstQuery.Where<MethodDeclaration>(ast, 
        m => m.Name.StartsWith("get") && m.Parameters.Count == 0);
    
    // Transform: Make all fields final
    var transformed = AstTransformer.ReplaceNodes<FieldDeclaration>(
        ast,
        f => !f.Modifiers.HasFlag(Modifiers.Final),
        f => {
            var newField = AstTransformer.Clone(f);
            newField.Modifiers |= Modifiers.Final;
            return newField;
        });
    
    // Visitor: Count nodes
    var counter = new NodeCounterVisitor();
    var nodeCount = ast.Accept(counter);
    Console.WriteLine($"Total nodes: {nodeCount}");
    
    // Serialize to JSON
    var json = AstJsonSerializer.Serialize(ast, indented: true);
    Console.WriteLine(json);
    
    // Deserialize from JSON
    var deserialized = AstJsonSerializer.Deserialize<CompilationUnit>(json);
}
```

## Performance Considerations

1. **Parsing**: O(n) where n is the length of source code
2. **Querying**: O(n) where n is the number of nodes in AST
3. **Transformation**: O(n) for cloning, varies for specific transformations
4. **Serialization**: O(n) for both serialization and deserialization

## Thread Safety

- `JavaParser.Parse()` is thread-safe (static method)
- AST nodes are not thread-safe for modification
- Visitors and queries are thread-safe for read-only operations
- Clone AST before parallel modifications

## Limitations

1. Supports Java 17 syntax
2. Does not perform semantic analysis (no type resolution)
3. Does not handle malformed Java files beyond basic error recovery
4. Comments are not preserved in the AST (they're available through token stream)

## Migration from IronJava v1.x

Update namespace references:
- `IronJava` → `MarketAlly.IronJava`
- `IronJava.Core` → `MarketAlly.IronJava.Core`
- All other namespaces follow the same pattern