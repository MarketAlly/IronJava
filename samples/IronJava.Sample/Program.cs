using IronJava.Core;
using IronJava.Core.AST;
using IronJava.Core.AST.Nodes;
using IronJava.Core.AST.Query;
using IronJava.Core.AST.Transformation;
using IronJava.Core.AST.Visitors;
using IronJava.Core.Serialization;

namespace IronJava.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("IronJava Sample Application");
            Console.WriteLine("===========================\n");

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