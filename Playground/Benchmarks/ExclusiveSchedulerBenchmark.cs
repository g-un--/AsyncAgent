using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Playground
{
    [MemoryDiagnoser]
    [IterationCount(10)]
    [WarmupCount(1)]
    public class ExclusiveSchedulerBenchmark
    {
        class AgentState
        {
            public long ItemsCount { get; set; }
            public long Sum { get; set; }
        }

        [Benchmark]
        public void Run()
        {
            var exclusiveScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            var agentState = new AgentState();
            var tcs = new TaskCompletionSource<AgentState>();

            foreach (var msg in Enumerable.Range(1, 1000000))
            {
                var currentTaskMessage = msg;
                Task.Factory.StartNew(() =>
                {
                    agentState.ItemsCount += 1;
                    agentState.Sum += currentTaskMessage;

                    if (agentState.ItemsCount == 1000000)
                    {
                        tcs.SetResult(agentState);
                    }
                }, CancellationToken.None, TaskCreationOptions.None, exclusiveScheduler);
            }

            var latestState = tcs.Task.Result;

            if (latestState.Sum != 500000500000)
            {
                throw new Exception("Sum should be 500000500000");
            }
            if (latestState.ItemsCount != 1000000)
            {
                throw new Exception("ItemsCount should be 10000000");
            }
        }
    }
}
