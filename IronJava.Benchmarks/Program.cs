using BenchmarkDotNet.Running;

namespace MarketAlly.IronJava.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ParsingBenchmarks>();
            BenchmarkRunner.Run<AstTraversalBenchmarks>();
            BenchmarkRunner.Run<TransformationBenchmarks>();
        }
    }
}