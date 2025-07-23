using System.Linq;
using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Query;
using Xunit;

namespace MarketAlly.IronJava.Tests
{
    public class AnnotationParsingTests
    {
        [Fact]
        public void TestClassAnnotations()
        {
            var javaCode = @"
                @Entity
                @Table(name = ""users"")
                public class User {
                    private String name;
                }
            ";

            var result = JavaParser.Parse(javaCode);
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);

            var userClass = result.Ast.Types.OfType<ClassDeclaration>().FirstOrDefault();
            Assert.NotNull(userClass);
            Assert.Equal("User", userClass.Name);
            
            // Check annotations
            Assert.Equal(2, userClass.Annotations.Count);
            
            var entityAnnotation = userClass.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "Entity");
            Assert.NotNull(entityAnnotation);
            Assert.Empty(entityAnnotation.Arguments);
            
            var tableAnnotation = userClass.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "Table");
            Assert.NotNull(tableAnnotation);
            Assert.Single(tableAnnotation.Arguments);
            
            var nameArg = tableAnnotation.Arguments[0] as AnnotationValueArgument;
            Assert.NotNull(nameArg);
            Assert.Equal("name", nameArg.Name);
        }

        [Fact]
        public void TestMethodAnnotations()
        {
            var javaCode = @"
                public class UserService {
                    @GetMapping(""/users/{id}"")
                    @ResponseBody
                    public User getUser(@PathVariable Long id) {
                        return null;
                    }
                }
            ";

            var result = JavaParser.Parse(javaCode);
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);

            var method = AstQuery.FindFirst<MethodDeclaration>(result.Ast);
            Assert.NotNull(method);
            Assert.Equal("getUser", method.Name);
            
            // Check method annotations
            Assert.Equal(2, method.Annotations.Count);
            
            var getMappingAnnotation = method.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "GetMapping");
            Assert.NotNull(getMappingAnnotation);
            Assert.Single(getMappingAnnotation.Arguments);
            
            var responseBodyAnnotation = method.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "ResponseBody");
            Assert.NotNull(responseBodyAnnotation);
            Assert.Empty(responseBodyAnnotation.Arguments);
        }

        [Fact]
        public void TestFieldAnnotations()
        {
            var javaCode = @"
                public class User {
                    @Id
                    @GeneratedValue(strategy = GenerationType.IDENTITY)
                    private Long id;
                    
                    @Column(nullable = false, length = 100)
                    private String name;
                }
            ";

            var result = JavaParser.Parse(javaCode);
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);

            var fields = AstQuery.FindAll<FieldDeclaration>(result.Ast).ToList();
            Assert.Equal(2, fields.Count);
            
            // Check id field annotations
            var idField = fields.FirstOrDefault(f => f.Variables[0].Name == "id");
            Assert.NotNull(idField);
            Assert.Equal(2, idField.Annotations.Count);
            
            var idAnnotation = idField.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "Id");
            Assert.NotNull(idAnnotation);
            
            var generatedValueAnnotation = idField.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "GeneratedValue");
            Assert.NotNull(generatedValueAnnotation);
            Assert.Single(generatedValueAnnotation.Arguments);
            
            // Check name field annotations
            var nameField = fields.FirstOrDefault(f => f.Variables[0].Name == "name");
            Assert.NotNull(nameField);
            Assert.Single(nameField.Annotations);
            
            var columnAnnotation = nameField.Annotations[0];
            Assert.Equal("Column", ((ClassOrInterfaceType)columnAnnotation.Type).Name);
            Assert.Equal(2, columnAnnotation.Arguments.Count);
        }

        [Fact]
        public void TestParameterAnnotations()
        {
            var javaCode = @"
                public class UserController {
                    public void updateUser(@RequestBody User user, @PathVariable Long id) {
                    }
                }
            ";

            var result = JavaParser.Parse(javaCode);
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);

            var method = AstQuery.FindFirst<MethodDeclaration>(result.Ast);
            Assert.NotNull(method);
            Assert.Equal(2, method.Parameters.Count);
            
            // Check first parameter annotations
            var userParam = method.Parameters[0];
            Assert.Equal("user", userParam.Name);
            Assert.Single(userParam.Annotations);
            Assert.Equal("RequestBody", ((ClassOrInterfaceType)userParam.Annotations[0].Type).Name);
            
            // Check second parameter annotations
            var idParam = method.Parameters[1];
            Assert.Equal("id", idParam.Name);
            Assert.Single(idParam.Annotations);
            Assert.Equal("PathVariable", ((ClassOrInterfaceType)idParam.Annotations[0].Type).Name);
        }

        [Fact]
        public void TestNestedAnnotations()
        {
            var javaCode = @"
                @Target({ElementType.METHOD, ElementType.TYPE})
                @Retention(RetentionPolicy.RUNTIME)
                @interface MyAnnotation {
                    String value() default ""default"";
                }
            ";

            var result = JavaParser.Parse(javaCode);
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);

            var annotationDecl = result.Ast.Types.OfType<AnnotationDeclaration>().FirstOrDefault();
            Assert.NotNull(annotationDecl);
            Assert.Equal("MyAnnotation", annotationDecl.Name);
            
            // Check meta-annotations
            Assert.Equal(2, annotationDecl.Annotations.Count);
            
            var targetAnnotation = annotationDecl.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "Target");
            Assert.NotNull(targetAnnotation);
            Assert.Single(targetAnnotation.Arguments);
            
            var retentionAnnotation = annotationDecl.Annotations.FirstOrDefault(a => ((ClassOrInterfaceType)a.Type).Name == "Retention");
            Assert.NotNull(retentionAnnotation);
            Assert.Single(retentionAnnotation.Arguments);
        }

        [Fact]
        public void TestAnnotationArrayValues()
        {
            var javaCode = @"
                @SuppressWarnings({""unchecked"", ""rawtypes""})
                public class Test {
                }
            ";

            var result = JavaParser.Parse(javaCode);
            Assert.True(result.Success);
            Assert.NotNull(result.Ast);

            var testClass = result.Ast.Types.OfType<ClassDeclaration>().FirstOrDefault();
            Assert.NotNull(testClass);
            Assert.Single(testClass.Annotations);
            
            var suppressWarningsAnnotation = testClass.Annotations[0];
            Assert.Equal("SuppressWarnings", ((ClassOrInterfaceType)suppressWarningsAnnotation.Type).Name);
            Assert.Single(suppressWarningsAnnotation.Arguments);
            
            var valueArg = suppressWarningsAnnotation.Arguments[0] as AnnotationValueArgument;
            Assert.NotNull(valueArg);
            Assert.Equal("value", valueArg.Name);
            
            // The value should be an ArrayInitializer expression
            var arrayInit = valueArg.Value as ArrayInitializer;
            Assert.NotNull(arrayInit);
            Assert.Equal(2, arrayInit.Elements.Count);
        }
    }
}