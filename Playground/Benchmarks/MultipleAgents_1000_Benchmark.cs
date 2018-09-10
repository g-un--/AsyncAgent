using AsyncAgentLib.Reactive;
using BenchmarkDotNet.Attributes;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Playground
{
    [MemoryDiagnoser]
    [IterationCount(10)]
    [WarmupCount(1)]
    public class MultipleAgents_1000_Benchmark
    {
        [Benchmark]
        public void Run()
        {
            var agents = Enumerable.Range(1, 1000).Select(x =>
                                new ReactiveAsyncAgent<int, int>(
                                    0,
                                    (state, msg, ct) => Task.FromResult(state + msg),
                                    (_, ct) => Task.FromResult(true))).ToArray();


            Parallel.ForEach(agents, agent =>
            {
                for (var msg = 1; msg <= 1000; msg += 1)
                {
                    agent.Send(msg);
                }
            });

            Task.WaitAll(agents.Select(x => x.State.Where(state => state == 500500).FirstAsync().ToTask()).ToArray());

            foreach (var agent in agents)
            {
                agent.Dispose();
            }
        }
    }
}
