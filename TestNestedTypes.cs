using System;
using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST.Nodes;

class TestNestedTypes
{
    static void Main()
    {
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
            }
        ";

        var result = JavaParser.Parse(javaCode);
        
        if (result.Success)
        {
            Console.WriteLine("Parse successful!");
            var outerClass = result.Ast.Types[0] as ClassDeclaration;
            if (outerClass != null)
            {
                Console.WriteLine($"Outer class: {outerClass.Name}");
                Console.WriteLine($"Number of members: {outerClass.Members.Count}");
                Console.WriteLine($"Number of nested types: {outerClass.NestedTypes.Count}");
                
                foreach (var nestedType in outerClass.NestedTypes)
                {
                    Console.WriteLine($"  - Nested type: {nestedType.Name} ({nestedType.GetType().Name})");
                }
            }
        }
        else
        {
            Console.WriteLine("Parse failed!");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error: {error}");
            }
        }
    }
}