using BenchmarkDotNet.Running;

namespace JSonTransformerBenchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
