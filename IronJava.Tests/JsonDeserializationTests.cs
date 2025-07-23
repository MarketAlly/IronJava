using System.Linq;
using System.Text.Json;
using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.Serialization;
using Xunit;

namespace MarketAlly.IronJava.Tests
{
    public class JsonDeserializationTests
    {
        private readonly AstJsonSerializer _serializer = new();

        [Fact]
        public void CanSerializeAndDeserializeSimpleClass()
        {
            var javaCode = @"
                package com.example;
                
                public class HelloWorld {
                    private String message;
                    
                    public HelloWorld(String msg) {
                        this.message = msg;
                    }
                    
                    public void printMessage() {
                        System.out.println(message);
                    }
                }
            ";

            // Parse Java code
            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);
            Assert.NotNull(parseResult.Ast);

            // Serialize to JSON
            var json = _serializer.Serialize(parseResult.Ast!);
            Assert.NotNull(json);

            // Deserialize back to AST
            var deserializedAst = _serializer.Deserialize<CompilationUnit>(json)!;
            Assert.NotNull(deserializedAst);

            // Verify structure
            Assert.NotNull(deserializedAst.Package);
            Assert.Equal("com.example", deserializedAst.Package.PackageName);
            Assert.Single(deserializedAst.Types);

            var classDecl = deserializedAst.Types[0] as ClassDeclaration;
            Assert.NotNull(classDecl);
            Assert.Equal("HelloWorld", classDecl.Name);
            Assert.True(classDecl.Modifiers.IsPublic());
            Assert.Equal(3, classDecl.Members.Count);

            // Check field
            var field = classDecl.Members[0] as FieldDeclaration;
            Assert.NotNull(field);
            Assert.True(field.Modifiers.IsPrivate());
            var fieldType = field.Type as ClassOrInterfaceType;
            Assert.NotNull(fieldType);
            Assert.Equal("String", fieldType.Name);

            // Check constructor
            var constructor = classDecl.Members[1] as MethodDeclaration;
            Assert.NotNull(constructor);
            Assert.True(constructor.IsConstructor);
            Assert.Equal("HelloWorld", constructor.Name);
            // Note: Current parser implementation doesn't capture constructor parameters
            // This is a parser limitation, not a JSON serialization issue

            // Check method
            var method = classDecl.Members[2] as MethodDeclaration;
            Assert.NotNull(method);
            Assert.Equal("printMessage", method.Name);
            Assert.True(method.Modifiers.IsPublic());
            Assert.NotNull(method.ReturnType); // void is represented as PrimitiveType with Kind = Void
            var returnType = method.ReturnType as PrimitiveType;
            Assert.NotNull(returnType);
            Assert.Equal(PrimitiveTypeKind.Void, returnType.Kind);
        }

        [Fact]
        public void CanSerializeAndDeserializeComplexExpressions()
        {
            var javaCode = @"
                public class ExpressionTest {
                    public void testExpressions() {
                        int a = 5;
                        int b = 10;
                        int c = a + b * 2;
                        boolean result = (a < b) && (c > 15);
                        String s = result ? ""yes"" : ""no"";
                        int[] arr = new int[]{1, 2, 3};
                        int elem = arr[0];
                        Object obj = (String) s;
                        boolean isString = obj instanceof String;
                    }
                }
            ";

            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);

            var json = _serializer.Serialize(parseResult.Ast!);
            var deserializedAst = _serializer.Deserialize<CompilationUnit>(json)!;

            Assert.NotNull(deserializedAst);
            var classDecl = deserializedAst.Types[0] as ClassDeclaration;
            Assert.NotNull(classDecl);
            var method = classDecl.Members[0] as MethodDeclaration;
            Assert.NotNull(method);
            Assert.NotNull(method.Body);

            // Verify we have statements
            Assert.True(method.Body.Statements.Count >= 9);

            // Check a binary expression
            var stmt3 = method.Body.Statements[2] as LocalVariableStatement;
            Assert.NotNull(stmt3);
            var init3 = stmt3.Variables[0].Initializer as BinaryExpression;
            Assert.NotNull(init3);
            Assert.Equal(BinaryOperator.Add, init3.Operator);

            // Check conditional expression
            var stmt5 = method.Body.Statements[4] as LocalVariableStatement;
            Assert.NotNull(stmt5);
            var init5 = stmt5.Variables[0].Initializer as ConditionalExpression;
            Assert.NotNull(init5);

            // Check array creation
            var stmt6 = method.Body.Statements[5] as LocalVariableStatement;
            Assert.NotNull(stmt6);
            Assert.NotNull(stmt6.Variables[0].Initializer); // Could be NewArrayExpression or ArrayInitializer

            // Check we can find specific expression types
            var hasConditional = method.Body.Statements.Any(s => 
                s is LocalVariableStatement lvs && lvs.Variables[0].Initializer is ConditionalExpression);
            Assert.True(hasConditional, "Should have a conditional expression");
            
            var hasArrayAccess = method.Body.Statements.Any(s => 
                s is LocalVariableStatement lvs && lvs.Variables[0].Initializer is ArrayAccessExpression);
            Assert.True(hasArrayAccess, "Should have an array access expression");

        }

        [Fact]
        public void CanSerializeAndDeserializeControlFlow()
        {
            var javaCode = @"
                public class ControlFlowTest {
                    public void testControlFlow(int n) {
                        if (n > 0) {
                            System.out.println(""positive"");
                        } else {
                            System.out.println(""non-positive"");
                        }
                        
                        while (n > 0) {
                            n--;
                        }
                        
                        do {
                            n++;
                        } while (n < 10);
                        
                        for (int i = 0; i < n; i++) {
                            if (i == 5) break;
                            if (i == 3) continue;
                            System.out.println(i);
                        }
                        
                        for (String s : new String[]{""a"", ""b"", ""c""}) {
                            System.out.println(s);
                        }
                        
                        switch (n) {
                            case 1:
                                System.out.println(""one"");
                                break;
                            case 2:
                            case 3:
                                System.out.println(""two or three"");
                                break;
                            default:
                                System.out.println(""other"");
                        }
                        
                        try {
                            throw new Exception(""test"");
                        } catch (Exception e) {
                            e.printStackTrace();
                        } finally {
                            System.out.println(""done"");
                        }
                    }
                }
            ";

            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);

            var json = _serializer.Serialize(parseResult.Ast!);
            var deserializedAst = _serializer.Deserialize<CompilationUnit>(json)!;

            Assert.NotNull(deserializedAst);
            var classDecl = deserializedAst.Types[0] as ClassDeclaration;
            Assert.NotNull(classDecl);
            var method = classDecl.Members[0] as MethodDeclaration;
            Assert.NotNull(method);
            Assert.NotNull(method.Body);

            // Verify control flow statements
            var statements = method.Body.Statements;
            
            // If statement
            Assert.IsType<IfStatement>(statements[0]);
            var ifStmt = (IfStatement)statements[0];
            Assert.NotNull(ifStmt.ElseStatement);

            // While statement
            Assert.IsType<WhileStatement>(statements[1]);

            // Do-while statement
            Assert.IsType<DoWhileStatement>(statements[2]);

            // For statement
            Assert.IsType<ForStatement>(statements[3]);
            var forStmt = (ForStatement)statements[3];
            Assert.Single(forStmt.Initializers);

            // For-each statement
            Assert.IsType<ForEachStatement>(statements[4]);

            // Switch statement
            Assert.IsType<SwitchStatement>(statements[5]);
            var switchStmt = (SwitchStatement)statements[5];
            Assert.Equal(3, switchStmt.Cases.Count); // case 1, case 2/3, default

            // Try statement
            Assert.IsType<TryStatement>(statements[6]);
            var tryStmt = (TryStatement)statements[6];
            Assert.Single(tryStmt.CatchClauses);
            Assert.NotNull(tryStmt.FinallyBlock);
        }

        [Fact]
        public void CanSerializeAndDeserializeEnum()
        {
            var javaCode = @"
                public enum Color {
                    RED(255, 0, 0),
                    GREEN(0, 255, 0),
                    BLUE(0, 0, 255);
                    
                    private final int r, g, b;
                    
                    Color(int r, int g, int b) {
                        this.r = r;
                        this.g = g;
                        this.b = b;
                    }
                    
                    public int getRed() { return r; }
                }
            ";

            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);

            var json = _serializer.Serialize(parseResult.Ast!);
            var deserializedAst = _serializer.Deserialize<CompilationUnit>(json)!;

            Assert.NotNull(deserializedAst);
            var enumDecl = deserializedAst.Types[0] as EnumDeclaration;
            Assert.NotNull(enumDecl);
            Assert.Equal("Color", enumDecl.Name);
            Assert.Equal(3, enumDecl.Constants.Count);

            // Check enum constants
            Assert.Equal("RED", enumDecl.Constants[0].Name);
            Assert.Equal(3, enumDecl.Constants[0].Arguments.Count);
        }

        [Fact]
        public void CanSerializeAndDeserializeLambdasAndMethodReferences()
        {
            var javaCode = @"
                import java.util.function.*;
                
                public class LambdaTest {
                    public void test() {
                        Function<String, Integer> f1 = s -> s.length();
                        BiFunction<String, String, String> f2 = (a, b) -> a + b;
                        Runnable r = () -> System.out.println(""Hello"");
                        
                        Function<String, Integer> ref1 = String::length;
                        Supplier<String> ref2 = ""test""::toString;
                        Function<Integer, String[]> ref3 = String[]::new;
                    }
                }
            ";

            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);

            var json = _serializer.Serialize(parseResult.Ast!);
            var deserializedAst = _serializer.Deserialize<CompilationUnit>(json)!;

            Assert.NotNull(deserializedAst);
            var classDecl = deserializedAst.Types[0] as ClassDeclaration;
            Assert.NotNull(classDecl);
            var method = classDecl.Members[0] as MethodDeclaration;
            Assert.NotNull(method);

            // Check lambda expressions
            var stmt1 = method.Body!.Statements[0] as LocalVariableStatement;
            Assert.NotNull(stmt1);
            var lambda1 = stmt1.Variables[0].Initializer as LambdaExpression;
            Assert.NotNull(lambda1);
            Assert.Single(lambda1.Parameters);

            var stmt2 = method.Body.Statements[1] as LocalVariableStatement;
            Assert.NotNull(stmt2);
            var lambda2 = stmt2.Variables[0].Initializer as LambdaExpression;
            Assert.NotNull(lambda2);
            Assert.Equal(2, lambda2.Parameters.Count);

            // Check method references
            var stmt4 = method.Body!.Statements[3] as LocalVariableStatement;
            Assert.NotNull(stmt4);
            var ref1 = stmt4!.Variables[0].Initializer as MethodReferenceExpression;
            Assert.NotNull(ref1);
            Assert.Equal("length", ref1!.MethodName);
        }

        [Fact]
        public void PreservesSourceLocationInformation()
        {
            var javaCode = @"public class Test {
    public void method() {
        int x = 42;
    }
}";

            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);

            var json = _serializer.Serialize(parseResult.Ast!);
            var deserializedAst = _serializer.Deserialize<CompilationUnit>(json)!;

            Assert.NotNull(deserializedAst);
            
            // Check that source location is preserved
            var classDecl = deserializedAst.Types[0] as ClassDeclaration;
            Assert.NotNull(classDecl);
            Assert.Equal(1, classDecl.Location.Start.Line);
            
            var method = classDecl.Members[0] as MethodDeclaration;
            Assert.NotNull(method);
            Assert.Equal(2, method.Location.Start.Line);
        }

        [Fact]
        public void HandlesNullValuesCorrectly()
        {
            var javaCode = @"
                public interface SimpleInterface {
                    void method();
                }
            ";

            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);

            var json = _serializer.Serialize(parseResult.Ast!);
            var deserializedAst = _serializer.Deserialize<CompilationUnit>(json)!;

            Assert.NotNull(deserializedAst);
            Assert.Null(deserializedAst.Package); // No package declaration
            Assert.Empty(deserializedAst.Imports); // No imports
            
            var interfaceDecl = deserializedAst.Types[0] as InterfaceDeclaration;
            Assert.NotNull(interfaceDecl);
            
            var method = interfaceDecl.Members[0] as MethodDeclaration;
            Assert.NotNull(method);
            Assert.Null(method.Body); // Interface method has no body
        }

        [Fact]
        public void RoundTripPreservesAstStructure()
        {
            var javaCode = @"
                package test.example;
                
                import java.util.*;
                import java.io.IOException;
                
                @SuppressWarnings(""unchecked"")
                public class CompleteExample<T extends Comparable<T>> {
                    private static final int CONSTANT = 42;
                    
                    @Deprecated
                    public T process(T input) throws IOException {
                        return input;
                    }
                }
            ";

            var parseResult = JavaParser.Parse(javaCode);
            Assert.True(parseResult.Success);
            var originalAst = parseResult.Ast;

            // First round trip
            var json1 = _serializer.Serialize(originalAst!);
            var deserialized1 = _serializer.Deserialize<CompilationUnit>(json1)!;

            // Second round trip
            var json2 = _serializer.Serialize(deserialized1);
            var deserialized2 = _serializer.Deserialize<CompilationUnit>(json2)!;

            // JSON should be identical after round trips
            Assert.Equal(json1, json2);

            // Verify structure is preserved
            Assert.Equal(originalAst!.Package!.PackageName, deserialized2!.Package!.PackageName);
            Assert.Equal(originalAst.Imports.Count, deserialized2.Imports.Count);
            Assert.Equal(originalAst.Types.Count, deserialized2.Types.Count);

            var origClass = originalAst.Types[0] as ClassDeclaration;
            var deserClass = deserialized2.Types[0] as ClassDeclaration;
            Assert.Equal(origClass!.Name, deserClass!.Name);
            Assert.Equal(origClass.TypeParameters.Count, deserClass.TypeParameters.Count);
            Assert.Equal(origClass.Members.Count, deserClass.Members.Count);
        }
    }
}