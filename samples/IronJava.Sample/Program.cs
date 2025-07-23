using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Query;
using MarketAlly.IronJava.Core.AST.Transformation;
using MarketAlly.IronJava.Core.AST.Visitors;
using MarketAlly.IronJava.Core.Serialization;

namespace IronJava.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("IronJava Sample Application");
            Console.WriteLine("===========================\n");

            // Test nested types
            TestNestedTypes();
            Console.WriteLine("\n===========================\n");
            
            // Test annotations
            TestAnnotations();
            Console.WriteLine("\n===========================\n");
            
            
            // Sample Java code
            string javaCode = @"
package com.example;

import java.util.List;
import java.util.ArrayList;

/**
 * Sample Java class for demonstration
 */
public class UserService {
    private final UserRepository repository;
    private static final String VERSION = ""1.0.0"";
    
    public UserService(UserRepository repository) {
        this.repository = repository;
    }
    
    public List<User> getAllUsers() {
        return repository.findAll();
    }
    
    public User getUserById(long id) {
        return repository.findById(id)
            .orElseThrow(() -> new UserNotFoundException(""User not found: "" + id));
    }
    
    private void validateUser(User user) {
        if (user.getName() == null || user.getName().isEmpty()) {
            throw new IllegalArgumentException(""User name cannot be empty"");
        }
    }
}

interface UserRepository {
    List<User> findAll();
    Optional<User> findById(long id);
}

class User {
    private long id;
    private String name;
    private String email;
    
    // Getters and setters
    public long getId() { return id; }
    public void setId(long id) { this.id = id; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
}
";

            // Parse the Java code
            var result = JavaParser.Parse(javaCode);

            if (!result.Success)
            {
                Console.WriteLine("Parsing failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - Line {error.Line}, Column {error.Column}: {error.Message}");
                }
                return;
            }

            var ast = result.Ast!;
            Console.WriteLine("✓ Successfully parsed Java code\n");

            // Demonstrate various features
            DemoBasicQueries(ast);
            DemoVisitorPattern(ast);
            DemoAstTransformation(ast);
            DemoJsonSerialization(ast);
            DemoPatternMatching(ast);
        }

        static void DemoBasicQueries(CompilationUnit ast)
        {
            Console.WriteLine("1. Basic AST Queries");
            Console.WriteLine("--------------------");

            // Count different types of declarations
            var classes = ast.FindAll<ClassDeclaration>().ToList();
            var interfaces = ast.FindAll<InterfaceDeclaration>().ToList();
            var methods = ast.FindAll<MethodDeclaration>().ToList();
            var fields = ast.FindAll<FieldDeclaration>().ToList();

            Console.WriteLine($"Classes: {classes.Count}");
            Console.WriteLine($"Interfaces: {interfaces.Count}");
            Console.WriteLine($"Methods: {methods.Count}");
            Console.WriteLine($"Fields: {fields.Count}");

            // List all class names
            Console.WriteLine("\nClass names:");
            foreach (var cls in classes)
            {
                Console.WriteLine($"  - {cls.Name}");
            }

            Console.WriteLine();
        }

        static void DemoVisitorPattern(CompilationUnit ast)
        {
            Console.WriteLine("2. Visitor Pattern Demo");
            Console.WriteLine("-----------------------");

            var analyzer = new CodeAnalyzer();
            ast.Accept(analyzer);

            Console.WriteLine($"Public methods: {analyzer.PublicMethodCount}");
            Console.WriteLine($"Private fields: {analyzer.PrivateFieldCount}");
            Console.WriteLine($"Total lines (approx): {analyzer.ApproximateLineCount}");
            Console.WriteLine();
        }

        static void DemoAstTransformation(CompilationUnit ast)
        {
            Console.WriteLine("3. AST Transformation Demo");
            Console.WriteLine("--------------------------");

            // Create a transformation that makes all classes final and renames a method
            var transformer = new TransformationBuilder()
                .AddModifier(Modifiers.Final)
                .RenameIdentifier("getUserById", "findUserById")
                ;

            var transformed = transformer.Transform(ast) as CompilationUnit;

            // Show the effect
            var originalClass = ast.FindFirst<ClassDeclaration>();
            var transformedClass = transformed?.FindFirst<ClassDeclaration>();

            Console.WriteLine($"Original class modifiers: {originalClass?.Modifiers}");
            Console.WriteLine($"Transformed class modifiers: {transformedClass?.Modifiers}");

            var originalMethod = ast.FindAll<MethodDeclaration>()
                .FirstOrDefault(m => m.Name == "getUserById");
            var transformedMethod = transformed?.FindAll<MethodDeclaration>()
                .FirstOrDefault(m => m.Name == "findUserById");

            Console.WriteLine($"\nMethod renamed: {originalMethod != null} -> {transformedMethod != null}");
            Console.WriteLine();
        }

        static void DemoJsonSerialization(CompilationUnit ast)
        {
            Console.WriteLine("4. JSON Serialization Demo");
            Console.WriteLine("--------------------------");

            var serializer = new AstJsonSerializer(indented: true);
            
            // Serialize just the first method for demo
            var firstMethod = ast.FindFirst<MethodDeclaration>();
            if (firstMethod != null)
            {
                var json = serializer.Serialize(firstMethod);
                
                // Show first few lines of JSON
                var lines = json.Split('\n').Take(10);
                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                }
                Console.WriteLine("... (truncated)");
            }
            Console.WriteLine();
        }

        static void DemoPatternMatching(CompilationUnit ast)
        {
            Console.WriteLine("5. Pattern Matching Demo");
            Console.WriteLine("------------------------");

            // Find getter and setter methods
            var allMethods = ast.FindAll<MethodDeclaration>().ToList();
            var getters = allMethods.Where(m => m.IsGetter()).ToList();
            var setters = allMethods.Where(m => m.IsSetter()).ToList();

            Console.WriteLine($"Getter methods: {getters.Count}");
            foreach (var getter in getters)
            {
                Console.WriteLine($"  - {getter.Name}");
            }

            Console.WriteLine($"\nSetter methods: {setters.Count}");
            foreach (var setter in setters)
            {
                Console.WriteLine($"  - {setter.Name}");
            }

            // Find methods that throw exceptions
            var throwingMethods = ast.Query<MethodDeclaration>()
                .Where(m => m.Throws.Any())
                .Execute()
                .ToList();

            Console.WriteLine($"\nMethods that throw exceptions: {throwingMethods.Count}");
            Console.WriteLine();
        }

        static void TestNestedTypes()
        {
            Console.WriteLine("Testing Nested Types Feature");
            Console.WriteLine("----------------------------");
            
            var javaCode = @"
public class OuterClass {
    private int outerField;
    
    public class InnerClass {
        private String innerField;
        
        public void innerMethod() {
            System.out.println(innerField);
        }
    }
    
    public static class StaticNestedClass {
        private static int staticField;
    }
    
    public interface NestedInterface {
        void doSomething();
    }
    
    public enum NestedEnum {
        VALUE1, VALUE2
    }
}
";

            var result = JavaParser.Parse(javaCode);
            
            if (result.Success)
            {
                Console.WriteLine("✓ Parse successful!");
                var outerClass = result.Ast!.Types[0] as ClassDeclaration;
                if (outerClass != null)
                {
                    Console.WriteLine($"\nOuter class: {outerClass.Name}");
                    Console.WriteLine($"  Members: {outerClass.Members.Count}");
                    Console.WriteLine($"  Nested types: {outerClass.NestedTypes.Count}");
                    
                    foreach (var member in outerClass.Members)
                    {
                        if (member is FieldDeclaration field)
                        {
                            Console.WriteLine($"    - Field: {field.Variables[0].Name}");
                        }
                    }
                    
                    foreach (var nestedType in outerClass.NestedTypes)
                    {
                        Console.WriteLine($"    - Nested: {nestedType.Name} ({nestedType.GetType().Name})");
                        
                        if (nestedType is ClassDeclaration nestedClass)
                        {
                            Console.WriteLine($"      Static: {nestedClass.Modifiers.HasFlag(Modifiers.Static)}");
                            Console.WriteLine($"      Members: {nestedClass.Members.Count}");
                        }
                        else if (nestedType is EnumDeclaration nestedEnum)
                        {
                            Console.WriteLine($"      Constants: {nestedEnum.Constants.Count}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("✗ Parse failed!");
            }
        }
        
        static void TestAnnotations()
        {
            Console.WriteLine("Testing Annotation Parsing");
            Console.WriteLine("--------------------------");
            
            var javaCode = @"
@Entity
@Table(name = ""users"")
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false, length = 100)
    private String name;
    
    @GetMapping(""/users/{id}"")
    @ResponseBody
    public User getUser(@PathVariable Long id) {
        return null;
    }
}
";

            var result = JavaParser.Parse(javaCode);
            
            if (result.Success)
            {
                Console.WriteLine("✓ Parse successful!");
                
                var userClass = result.Ast!.Types[0] as ClassDeclaration;
                if (userClass != null)
                {
                    Console.WriteLine($"\nClass: {userClass.Name}");
                    Console.WriteLine($"  Class annotations: {userClass.Annotations.Count}");
                    foreach (var annotation in userClass.Annotations)
                    {
                        var typeName = annotation.Type is ClassOrInterfaceType cit ? cit.Name : annotation.Type.ToString();
                        Console.WriteLine($"    - @{typeName} with {annotation.Arguments.Count} arguments");
                    }
                    
                    // Check fields
                    var fields = userClass.Members.OfType<FieldDeclaration>().ToList();
                    Console.WriteLine($"\n  Fields: {fields.Count}");
                    foreach (var field in fields)
                    {
                        Console.WriteLine($"    - {field.Variables[0].Name}: {field.Annotations.Count} annotations");
                        foreach (var annotation in field.Annotations)
                        {
                            var typeName = annotation.Type is ClassOrInterfaceType cit ? cit.Name : annotation.Type.ToString();
                            Console.WriteLine($"      - @{typeName}");
                        }
                    }
                    
                    // Check methods
                    var methods = userClass.Members.OfType<MethodDeclaration>().ToList();
                    Console.WriteLine($"\n  Methods: {methods.Count}");
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"    - {method.Name}: {method.Annotations.Count} annotations");
                        foreach (var annotation in method.Annotations)
                        {
                            var typeName = annotation.Type is ClassOrInterfaceType cit ? cit.Name : annotation.Type.ToString();
                            Console.WriteLine($"      - @{typeName}");
                        }
                        
                        // Check parameters
                        foreach (var param in method.Parameters)
                        {
                            if (param.Annotations.Count > 0)
                            {
                                Console.WriteLine($"      Parameter {param.Name}: {param.Annotations.Count} annotations");
                                foreach (var annotation in param.Annotations)
                                {
                                    var typeName = annotation.Type is ClassOrInterfaceType cit ? cit.Name : annotation.Type.ToString();
                                    Console.WriteLine($"        - @{typeName}");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("✗ Parse failed!");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error.Message} at {error.Line}:{error.Column}");
                }
            }
        }
    }

    // Custom visitor for code analysis
    class CodeAnalyzer : JavaVisitorBase
    {
        public int PublicMethodCount { get; private set; }
        public int PrivateFieldCount { get; private set; }
        public int ApproximateLineCount { get; private set; }

        public override void VisitMethodDeclaration(MethodDeclaration node)
        {
            if (node.Modifiers.HasFlag(Modifiers.Public))
            {
                PublicMethodCount++;
            }

            // Approximate line count based on location
            var lines = node.Location.End.Line - node.Location.Start.Line + 1;
            ApproximateLineCount += lines;

            base.VisitMethodDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclaration node)
        {
            if (node.Modifiers.HasFlag(Modifiers.Private))
            {
                PrivateFieldCount++;
            }

            base.VisitFieldDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclaration node)
        {
            var lines = node.Location.End.Line - node.Location.Start.Line + 1;
            ApproximateLineCount += lines;

            base.VisitClassDeclaration(node);
        }
    }
}