using AsyncAgentLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokenSource = new CancellationTokenSource();
            TestPerformance(tokenSource.Token);
            Console.ReadLine();
            tokenSource.Cancel();
        }

        private static Task TestPerformance(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var stopwatch = new Stopwatch();
                long sum = 0;
                int items = 0;
                int test = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var agent = new AsyncAgent<long>(async (msg, ct) =>
                    {
                        sum += msg;
                        items += 1;
                        await Task.Delay(0);
                        if (items == 1000000)
                        {
                            stopwatch.Stop();
                            Console.WriteLine("Items: [1..{0}], Sum: {1}, Time: {2}ms", items, sum, stopwatch.ElapsedMilliseconds);
                            stopwatch.Reset();
                            sum = 0;
                            items = 0;
                        }
                    });

                    test += 1;
                    stopwatch.Start();

                    for (long item = 1; item <= 1000000; item++)
                    {
                        long msg = item;

                        agent.Send(msg);
                    }

                    Thread.Sleep(1000);
                    agent.Dispose();

                    if (test % 10 == 0)
                    {
                        test = 0;
                        Console.Clear();
                        Thread.Sleep(1000);
                        stopwatch.Reset();
                    }
                }
            }, cancellationToken);
        }
    }
}
