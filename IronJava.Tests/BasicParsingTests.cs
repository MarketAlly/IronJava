using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST;
using MarketAlly.IronJava.Core.AST.Nodes;
using Xunit;

namespace MarketAlly.IronJava.Tests
{
    public class BasicParsingTests
    {
        [Fact]
        public void CanParseSimpleClass()
        {
            var javaCode = @"
                public class HelloWorld {
                    public static void main(String[] args) {
                        System.out.println(""Hello, World!"");
                    }
                }
            ";
            
            var result = JavaParser.Parse(javaCode);
            
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);
            Assert.Single(result.Ast.Types);
            
            var classDecl = result.Ast.Types[0] as ClassDeclaration;
            Assert.NotNull(classDecl);
            Assert.Equal("HelloWorld", classDecl.Name);
            Assert.True(classDecl.Modifiers.IsPublic());
            Assert.Single(classDecl.Members);
            
            var method = classDecl.Members[0] as MethodDeclaration;
            Assert.NotNull(method);
            Assert.Equal("main", method.Name);
            Assert.True(method.Modifiers.IsPublic());
            Assert.True(method.Modifiers.IsStatic());
        }
        
        [Fact]
        public void CanParsePackageDeclaration()
        {
            var javaCode = @"
                package com.example;
                
                public class Test {
                }
            ";
            
            var result = JavaParser.Parse(javaCode);
            
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);
            Assert.NotNull(result.Ast.Package);
            Assert.Equal("com.example", result.Ast.Package.PackageName);
            Assert.Single(result.Ast.Types);
            
            var classDecl = result.Ast.Types[0] as ClassDeclaration;
            Assert.NotNull(classDecl);
            Assert.Equal("Test", classDecl.Name);
        }
        
        [Fact]
        public void CanParseInterface()
        {
            var javaCode = @"
                public interface Runnable {
                    void run();
                }
            ";
            
            var result = JavaParser.Parse(javaCode);
            
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);
            Assert.Single(result.Ast.Types);
            
            var interfaceDecl = result.Ast.Types[0] as InterfaceDeclaration;
            Assert.NotNull(interfaceDecl);
            Assert.Equal("Runnable", interfaceDecl.Name);
            Assert.Single(interfaceDecl.Members);
            
            var method = interfaceDecl.Members[0] as MethodDeclaration;
            Assert.NotNull(method);
            Assert.Equal("run", method.Name);
            Assert.Null(method.Body); // Interface method has no body
        }
        
        [Fact]
        public void CanParseEnum()
        {
            var javaCode = @"
                public enum Color {
                    RED, GREEN, BLUE
                }
            ";
            
            var result = JavaParser.Parse(javaCode);
            
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);
            Assert.Single(result.Ast.Types);
            
            var enumDecl = result.Ast.Types[0] as EnumDeclaration;
            Assert.NotNull(enumDecl);
            Assert.Equal("Color", enumDecl.Name);
            Assert.Equal(3, enumDecl.Constants.Count);
            Assert.Equal("RED", enumDecl.Constants[0].Name);
            Assert.Equal("GREEN", enumDecl.Constants[1].Name);
            Assert.Equal("BLUE", enumDecl.Constants[2].Name);
        }
    }
}