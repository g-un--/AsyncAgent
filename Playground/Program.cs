using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Linq;
using System.Reactive.Linq;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = ConsoleLogger.Default;
            var config = new ManualConfig();
            config.Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            config.Add(DefaultConfig.Instance.GetExporters().ToArray());
            config.Add(DefaultConfig.Instance.GetDiagnosers().ToArray());
            config.Add(DefaultConfig.Instance.GetAnalysers().ToArray());
            config.Add(DefaultConfig.Instance.GetJobs().ToArray());
            config.Add(DefaultConfig.Instance.GetValidators().ToArray());
            config.Add(NullLogger.Instance);
            config.UnionRule = ConfigUnionRule.AlwaysUseGlobal;

            RunBenchmark<SkynetBenchmark>(config, logger);
            RunBenchmark<MultipleAgents_1000_Benchmark>(config, logger);
            RunBenchmark<ReactiveAsyncAgentBenchmark>(config, logger);
            RunBenchmark<AsyncAgentBenchmark>(config, logger);
        }

        public static void RunBenchmark<T>(ManualConfig config, ILogger logger)
        {
            logger.WriteLineHeader(typeof(T).Name);
            var summary = BenchmarkRunner.Run<T>(config);
            MarkdownExporter.Console.ExportToLog(summary, logger);
            logger.WriteLine();
        }
    }
}
