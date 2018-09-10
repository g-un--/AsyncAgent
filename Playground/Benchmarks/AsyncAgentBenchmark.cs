using AsyncAgentLib;
using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Playground
{
    [MemoryDiagnoser]
    [IterationCount(10)]
    [WarmupCount(1)]
    public class AsyncAgentBenchmark
    {
        class AgentState
        {
            public long ItemsCount { get; set; }
            public long Sum { get; set; }
        }

        [Benchmark]
        public void Run()
        {
            var tcs = new TaskCompletionSource<AgentState>();
            var agent =  new AsyncAgent<AgentState, int>(
                initialState: new AgentState { ItemsCount = 0, Sum = 0 },
                messageHandler: (state, msg, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    state.Sum += msg;
                    state.ItemsCount = state.ItemsCount + 1;
                    if (state.ItemsCount == 1000000)
                    {
                        tcs.SetResult(state);
                    }
                    return Task.FromResult(state);

                },
                errorHandler: (ex, ct) => Task.FromResult(false));

            foreach (var msg in Enumerable.Range(1, 1000000))
            {
                agent.Send(msg);
            }
            var latestState = tcs.Task.Result;
            agent.Dispose();

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
