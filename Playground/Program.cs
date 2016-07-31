using AsyncAgentLib;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokenSource = new CancellationTokenSource();

            TestGate(tokenSource.Token)
                .ContinueWith(task => TestPerformance(tokenSource.Token));

            ReadLine();
            tokenSource.Cancel();
        }

        private static Task TestGate(CancellationToken ct)
        {
            return Task.Run(async () =>
            {
                var gate = new Gate<string>(LockState.Locked, async m =>
                {
                    await Task.Delay(0);
                    WriteLine(m);
                });

                gate.Send("This message should not be displayed.");
                gate.Unlock();
                gate.Send("Hello World!");
                gate.Lock();
                gate.Send("This message should not be displayed.");

                await Task.Delay(2000);
                gate.Dispose();
                Clear();
            }, ct);
        }

        private static Task TestPerformance(CancellationToken ct)
        {
            return Task.Run(async () =>
            {
                var stopwatch = new Stopwatch();
                
                int test = 0;
                while (!ct.IsCancellationRequested)
                {
                    test += 1;

                    var agent = GetNewAgent(stopwatch);
                    stopwatch.Start();

                    for (long item = 1; item <= 1000000; item++)
                    {
                        long msg = item;
                        agent.Send(msg);
                    }

                    await Task.Delay(1000, ct);
                    agent.Dispose();

                    if (test % 10 == 0)
                    {
                        test = 0;
                        Clear();
                        await Task.Delay(1000, ct);
                        stopwatch.Reset();
                    }
                }
            }, ct);
        }

        static AsyncAgent<long> GetNewAgent(Stopwatch stopwatch)
        {
            long sum = 0;
            int items = 0;

            return new AsyncAgent<long>(async (msg, ct) =>
            {
                sum += msg;
                items += 1;
                await Task.Delay(0);
                if (items == 1000000)
                {
                    stopwatch.Stop();
                    WriteLine($"Items: [1..{items}], Sum: {sum}, Time: {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Reset();
                    sum = 0;
                    items = 0;
                }
            });
        }
    }
}
