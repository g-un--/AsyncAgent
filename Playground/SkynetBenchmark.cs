using AsyncAgentLib;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Playground
{
    public static class SkynetBenchmark
    {
        public static Task<Tuple<long, long>> Run()
        {
            var tcs = new TaskCompletionSource<Tuple<long, long>>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            LaunchSkynet(0, 1000000, (rez) =>
            {
                stopwatch.Stop();
                tcs.SetResult(new Tuple<long, long>(rez, stopwatch.ElapsedMilliseconds));
            });

            return tcs.Task;
        }

        /// <summary>
        /// https://github.com/atemerev/skynet
        /// https://github.com/atemerev/skynet/blob/master/fsharp_agent/skynet.fsx
        /// </summary>
        private static void LaunchSkynet(long num, long size, Action<long> postback)
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
