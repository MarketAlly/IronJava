using System;
using System.Linq;
using MarketAlly.IronJava.Core.AST;
using MarketAlly.IronJava.Core.AST.Comparison;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Query;
using MarketAlly.IronJava.Core.AST.Transformation;
using MarketAlly.IronJava.Core.Serialization;
using Xunit;

namespace MarketAlly.IronJava.Tests
{
    public class Phase3Tests
    {
        private static SourceRange CreateLocation() => new(
            new SourceLocation(1, 1, 0, 0),
            new SourceLocation(1, 1, 0, 0)
        );

        [Fact]
        public void JsonSerializationWorks()
        {
            // Arrange
            var location = CreateLocation();
            var method = new MethodDeclaration(
                location,
                "testMethod",
                Modifiers.Public | Modifiers.Static,
                new List<Annotation>(),
                new PrimitiveType(location, PrimitiveTypeKind.Void),
                new List<TypeParameter>(),
                new List<Parameter>
                {
                    new Parameter(
                        location,
                        new PrimitiveType(location, PrimitiveTypeKind.Int),
                        "param1",
                        false,
                        false,
                        new List<Annotation>()
                    )
                },
                new List<TypeReference>(),
                new BlockStatement(location, new List<Statement>()),
                null
            );

            var serializer = new AstJsonSerializer(indented: false);

            // Act
            var json = serializer.Serialize(method);

            // Assert
            Assert.Contains("\"nodeType\":\"MethodDeclaration\"", json);
            Assert.Contains("\"name\":\"testMethod\"", json);
            Assert.Contains("\"modifiers\":[\"public\",\"static\"]", json);
            Assert.Contains("\"parameters\"", json);
            Assert.Contains("\"param1\"", json);
        }

        [Fact]
        public void AstQueryFindAllWorks()
        {
            // Arrange
            var location = CreateLocation();
            var compilation = CreateSampleCompilationUnit();

            // Act
            var methods = compilation.FindAll<MethodDeclaration>().ToList();
            var fields = compilation.FindAll<FieldDeclaration>().ToList();
            var classes = compilation.FindAll<ClassDeclaration>().ToList();

            // Assert
            Assert.Equal(2, methods.Count);
            Assert.Single(fields);
            Assert.Single(classes);
        }

        [Fact]
        public void AstQueryWhereWorks()
        {
            // Arrange
            var compilation = CreateSampleCompilationUnit();

            // Act
            var publicMethods = compilation
                .Where<MethodDeclaration>(m => m.Modifiers.IsPublic())
                .ToList();

            var staticFields = compilation
                .Where<FieldDeclaration>(f => f.Modifiers.IsStatic())
                .ToList();

            // Assert
            Assert.Single(publicMethods);
            Assert.Equal("main", publicMethods[0].Name);
            Assert.Empty(staticFields);
        }

        [Fact]
        public void AstQueryBuilderWorks()
        {
            // Arrange
            var compilation = CreateSampleCompilationUnit();

            // Act
            var result = compilation
                .Query<MethodDeclaration>()
                .WithName("main")
                .WithModifier(Modifiers.Public)
                .WithModifier(Modifiers.Static)
                .ExecuteFirst();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("main", result.Name);
        }

        [Fact]
        public void AstPatternMatchingWorks()
        {
            // Arrange
            var compilation = CreateSampleCompilationUnit();
            var methods = compilation.FindAll<MethodDeclaration>().ToList();

            // Act & Assert
            Assert.True(methods[0].IsMainMethod()); // main method
            Assert.False(methods[1].IsGetter()); // helper method
        }

        [Fact]
        public void AstTransformationRenameWorks()
        {
            // Arrange
            var location = CreateLocation();
            var original = new IdentifierExpression(location, "oldName");
            var transformer = new IdentifierRenamer("oldName", "newName");

            // Act
            var transformed = original.Accept(transformer) as IdentifierExpression;

            // Assert
            Assert.NotNull(transformed);
            Assert.Equal("newName", transformed.Name);
        }

        [Fact]
        public void AstTransformationModifierWorks()
        {
            // Arrange
            var compilation = CreateSampleCompilationUnit();
            var transformer = ModifierTransformer.AddModifier(Modifiers.Final);

            // Act
            var transformed = compilation.Accept(transformer) as CompilationUnit;
            var transformedClass = transformed?.Types[0] as ClassDeclaration;

            // Assert
            Assert.NotNull(transformedClass);
            Assert.True(transformedClass.Modifiers.IsFinal());
        }

        [Fact]
        public void AstEqualityComparerWorks()
        {
            // Arrange
            var location1 = CreateLocation();
            var location2 = new SourceRange(
                new SourceLocation(2, 2, 10, 0),
                new SourceLocation(2, 2, 10, 0)
            );

            var expr1 = new IdentifierExpression(location1, "test");
            var expr2 = new IdentifierExpression(location1, "test");
            var expr3 = new IdentifierExpression(location2, "test");
            var expr4 = new IdentifierExpression(location1, "different");

            var comparer = new AstEqualityComparer(ignoreLocation: true);

            // Act & Assert
            Assert.True(comparer.Equals(expr1, expr2));
            Assert.True(comparer.Equals(expr1, expr3)); // Different location, but ignored
            Assert.False(comparer.Equals(expr1, expr4)); // Different name
        }

        [Fact]
        public void AstDifferWorks()
        {
            // Arrange
            var location = CreateLocation();
            var original = new BlockStatement(location, new List<Statement>
            {
                new ExpressionStatement(location, new IdentifierExpression(location, "a")),
                new ExpressionStatement(location, new IdentifierExpression(location, "b"))
            });

            var modified = new BlockStatement(location, new List<Statement>
            {
                new ExpressionStatement(location, new IdentifierExpression(location, "a")),
                new ExpressionStatement(location, new IdentifierExpression(location, "c")), // Changed
                new ExpressionStatement(location, new IdentifierExpression(location, "d"))  // Added
            });

            var differ = new AstDiffer();

            // Act
            var diff = differ.ComputeDiff(original, modified);

            // Assert
            Assert.False(diff.IsEmpty);
            
            // The differ detects:
            // 1. Modification of the second ExpressionStatement (b -> c)
            // 2. Modification of the IdentifierExpression inside it (b -> c) 
            // 3. Addition of the third ExpressionStatement (d)
            // 4. Addition of the IdentifierExpression inside it (d)
            // 5. Addition of the fourth statement's expression
            // Total = 5 changes when comparing deeply
            Assert.Equal(5, diff.TotalChanges);
        }

        [Fact]
        public void TransformationBuilderWorks()
        {
            // Arrange
            var location = CreateLocation();
            var classDecl = new ClassDeclaration(
                location,
                "TestClass",
                Modifiers.Public,
                new List<Annotation>(),
                new List<TypeParameter>(),
                null,
                new List<TypeReference>(),
                new List<MemberDeclaration>
                {
                    new FieldDeclaration(
                        location,
                        Modifiers.Private,
                        new List<Annotation>(),
                        new PrimitiveType(location, PrimitiveTypeKind.Int),
                        new List<VariableDeclarator>
                        {
                            new VariableDeclarator(location, "oldField", 0, null)
                        },
                        null
                    )
                },
                null
            );

            var builder = new TransformationBuilder()
                .AddModifier(Modifiers.Final)
                .RenameIdentifier("oldField", "newField");

            // Act
            var transformed = builder.Transform(classDecl) as ClassDeclaration;

            // Assert
            Assert.NotNull(transformed);
            Assert.True(transformed.Modifiers.IsFinal());
            var field = transformed.Members[0] as FieldDeclaration;
            Assert.NotNull(field);
            Assert.Equal("newField", field.Variables[0].Name);
        }

        [Fact]
        public void ComplexQueryScenario()
        {
            // Arrange
            var compilation = CreateComplexCompilationUnit();

            // Act
            // Find all public static methods in classes that implement Serializable
            var query = compilation
                .QueryClasses()
                .Where(c => c.Interfaces.Any(i => (i as ClassOrInterfaceType)?.Name == "Serializable"))
                .Execute()
                .SelectMany(c => c.Members.OfType<MethodDeclaration>())
                .Where(m => m.Modifiers.IsPublic() && m.Modifiers.IsStatic())
                .ToList();

            // Assert
            Assert.Single(query);
            Assert.Equal("serialize", query[0].Name);
        }

        private CompilationUnit CreateSampleCompilationUnit()
        {
            var location = CreateLocation();

            var mainMethod = new MethodDeclaration(
                location,
                "main",
                Modifiers.Public | Modifiers.Static,
                new List<Annotation>(),
                new PrimitiveType(location, PrimitiveTypeKind.Void),
                new List<TypeParameter>(),
                new List<Parameter>
                {
                    new Parameter(
                        location,
                        new ArrayType(location, 
                            new ClassOrInterfaceType(location, "String", null, new List<TypeArgument>(), new List<Annotation>()), 
                            1),
                        "args",
                        false,
                        false,
                        new List<Annotation>()
                    )
                },
                new List<TypeReference>(),
                new BlockStatement(location, new List<Statement>()),
                null
            );

            var helperMethod = new MethodDeclaration(
                location,
                "helper",
                Modifiers.Private,
                new List<Annotation>(),
                new PrimitiveType(location, PrimitiveTypeKind.Void),
                new List<TypeParameter>(),
                new List<Parameter>(),
                new List<TypeReference>(),
                new BlockStatement(location, new List<Statement>()),
                null
            );

            var field = new FieldDeclaration(
                location,
                Modifiers.Private,
                new List<Annotation>(),
                new PrimitiveType(location, PrimitiveTypeKind.Int),
                new List<VariableDeclarator>
                {
                    new VariableDeclarator(location, "count", 0, null)
                },
                null
            );

            var testClass = new ClassDeclaration(
                location,
                "TestClass",
                Modifiers.Public,
                new List<Annotation>(),
                new List<TypeParameter>(),
                null,
                new List<TypeReference>(),
                new List<MemberDeclaration> { mainMethod, helperMethod, field },
                null
            );

            return new CompilationUnit(
                location,
                null,
                new List<ImportDeclaration>(),
                new List<TypeDeclaration> { testClass }
            );
        }

        private CompilationUnit CreateComplexCompilationUnit()
        {
            var location = CreateLocation();

            var serializableInterface = new ClassOrInterfaceType(
                location, 
                "Serializable", 
                null, 
                new List<TypeArgument>(), 
                new List<Annotation>()
            );

            var serializeMethod = new MethodDeclaration(
                location,
                "serialize",
                Modifiers.Public | Modifiers.Static,
                new List<Annotation>(),
                new PrimitiveType(location, PrimitiveTypeKind.Void),
                new List<TypeParameter>(),
                new List<Parameter>(),
                new List<TypeReference>(),
                null,
                null
            );

            var dataClass = new ClassDeclaration(
                location,
                "DataClass",
                Modifiers.Public,
                new List<Annotation>(),
                new List<TypeParameter>(),
                null,
                new List<TypeReference> { serializableInterface },
                new List<MemberDeclaration> { serializeMethod },
                null
            );

            return new CompilationUnit(
                location,
                null,
                new List<ImportDeclaration>(),
                new List<TypeDeclaration> { dataClass }
            );
        }
    }
}