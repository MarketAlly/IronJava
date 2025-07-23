using BenchmarkDotNet.Attributes;
using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Query;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Benchmarks
{
    [MemoryDiagnoser]
    public class AstTraversalBenchmarks
    {
        private CompilationUnit _ast = null!;
        private CountingVisitor _visitor = null!;

        [GlobalSetup]
        public void Setup()
        {
            var javaCode = GenerateComplexJavaCode();
            var result = JavaParser.Parse(javaCode);
            
            if (!result.Success)
                throw new Exception("Failed to parse test code");
                
            _ast = result.Ast!;
            _visitor = new CountingVisitor();
        }

        [Benchmark]
        public void TraverseWithVisitor()
        {
            _visitor.Reset();
            _ast.Accept(_visitor);
        }

        [Benchmark]
        public void FindAllMethods()
        {
            var methods = _ast.FindAll<MethodDeclaration>().ToList();
        }

        [Benchmark]
        public void QueryPublicMethods()
        {
            var methods = _ast.Query<MethodDeclaration>()
                .WithModifier(Modifiers.Public)
                .Execute()
                .ToList();
        }

        [Benchmark]
        public void FindMethodsWithLinq()
        {
            var methods = _ast.FindAll<MethodDeclaration>()
                .Where(m => m.Modifiers.IsPublic() && !m.Modifiers.IsStatic())
                .ToList();
        }

        [Benchmark]
        public void ComplexQuery()
        {
            var results = _ast.FindAll<ClassDeclaration>()
                .SelectMany(c => c.Members.OfType<MethodDeclaration>())
                .Where(m => m.Parameters.Count > 2)
                .Where(m => m.Modifiers.IsPublic())
                .Select(m => new { m.Name, ParamCount = m.Parameters.Count })
                .ToList();
        }

        private string GenerateComplexJavaCode()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("package benchmark.test;");
            sb.AppendLine();
            
            // Generate multiple classes
            for (int c = 0; c < 10; c++)
            {
                sb.AppendLine($"public class TestClass{c} {{");
                
                // Fields
                for (int f = 0; f < 5; f++)
                {
                    sb.AppendLine($"    private String field{f};");
                }
                
                sb.AppendLine();
                
                // Methods
                for (int m = 0; m < 10; m++)
                {
                    sb.AppendLine($"    public void method{m}(String param1, int param2, Object param3) {{");
                    sb.AppendLine($"        System.out.println(\"Method {m}\");");
                    sb.AppendLine($"    }}");
                    sb.AppendLine();
                }
                
                sb.AppendLine("}");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        private class CountingVisitor : JavaVisitorBase
        {
            public int NodeCount { get; private set; }

            public void Reset() => NodeCount = 0;

            protected override void DefaultVisit(MarketAlly.IronJava.Core.AST.JavaNode node)
            {
                NodeCount++;
                base.DefaultVisit(node);
            }
        }
    }
}