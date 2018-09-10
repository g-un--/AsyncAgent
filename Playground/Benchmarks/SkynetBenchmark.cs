using AsyncAgentLib;
using BenchmarkDotNet.Attributes;
using System;
using System.Threading.Tasks;

namespace Playground
{
    [MemoryDiagnoser]
    [IterationCount(10)]
    [WarmupCount(1)]
    public class SkynetBenchmark
    {
        [Benchmark]
        public void Run()
        {
            var tcs = new TaskCompletionSource<long>();

            LaunchSkynet(0, 1000000, (rez) =>
            {
                tcs.SetResult(rez);
            });

            tcs.Task.Wait();

            if (tcs.Task.Result != 499999500000)
            {
                throw new Exception("The answer should be 499999500000");
            }
        }

        /// <summary>
        /// https://github.com/atemerev/skynet
        /// https://github.com/atemerev/skynet/blob/master/fsharp_agent/skynet.fsx
        /// </summary>
        private void LaunchSkynet(long num, long size, Action<long> postback)
        {
            if (size == 1)
            {
                postback(num);
            }
            else
            {
                var agent = new AsyncAgent<Tuple<long, long>, long>(new Tuple<long, long>(0, 10), (state, msg, ct) =>
                {
                    var newState = new Tuple<long, long>(state.Item1 + msg, state.Item2 - 1);
                    if (newState.Item2 == 0)
                    {
                        postback(state.Item1 + msg);
                    }
                    return Task.FromResult(newState);
                }, (_, __) => Task.FromResult(true));

                for (var i = 0; i <= 9; i++)
                {
                    var subSize = size / 10;
                    var subNum = num + (i * subSize);
                    LaunchSkynet(subNum, subSize, (val) => agent.Send(val));
                }
            }
        }
    }
}
