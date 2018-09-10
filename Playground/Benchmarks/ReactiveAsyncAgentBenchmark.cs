using AsyncAgentLib.Reactive;
using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Playground
{
    [MemoryDiagnoser]
    [IterationCount(10)]
    [WarmupCount(1)]
    public class ReactiveAsyncAgentBenchmark
    {
        class AgentState
        {
            public long ItemsCount { get; set; }
            public long Sum { get; set; }
        }

        [Benchmark]
        public void Run()
        {
            var agent = new ReactiveAsyncAgent<AgentState, int>(
                initialState: new AgentState { ItemsCount = 0, Sum = 0 },
                messageHandler: (state, msg, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    state.Sum += msg;
                    state.ItemsCount = state.ItemsCount + 1;

                    return Task.FromResult(state);
                },
                errorHandler: (ex, ct) => Task.FromResult(false));

            var completedMessagesTask = agent.State.SkipWhile(state => state.ItemsCount < 1000000).FirstAsync();
            foreach (var msg in Enumerable.Range(1, 1000000))
            {
                agent.Send(msg);
            }
            var latestState = completedMessagesTask.ToTask().Result;
            agent.Dispose();

            if(latestState.Sum != 500000500000)
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
