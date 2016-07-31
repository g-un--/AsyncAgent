using AsyncAgentLib;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncAgent.Tests
{
    public class AsyncAgentTests
    {
        [Fact]
        public void AgentThrowsArgumentNullException()
        {
            ArgumentNullException thrownException = null;

            try
            {
                new AsyncAgent<string>(null);
            }
            catch (ArgumentNullException ex)
            {
                thrownException = ex;
            }

            Assert.NotNull(thrownException);
        }

        [Fact]
        public async Task AgentCanHandleAMessage()
        {
            var tcs = new TaskCompletionSource<string>();
            var message = "test";
            var agent = new AsyncAgent<string>(async (msg, ct) =>
            {
                await Task.Delay(0); tcs.SetResult(msg);
            });

            agent.Send(message);
            var processedMessage = await tcs.Task;

            Assert.Equal(message, processedMessage);
        }

        [Fact]
        public async Task AgentTriggersErrorHandler()
        {
            var tcs = new TaskCompletionSource<Exception>();
            var message = "test";
            var exception = new Exception();
            var agent = new AsyncAgent<string>(async (msg, ct) =>
            {
                await Task.Delay(0);
                throw exception;
            });
            agent.Error += (ex) => tcs.SetResult(ex);

            agent.Send(message);
            var triggeredException = await tcs.Task;

            Assert.Equal(exception, triggeredException);
        }

        [Fact]
        public async Task AgentDoesNotHandleMessagesAfterDispose()
        {
            var cts = new TaskCompletionSource<int>();
            var agent = new AsyncAgent<int>(async (msg, ct) => { await Task.Delay(0); cts.SetResult(msg); });
            agent.Dispose();
            agent.Send(1);

            await Task.Delay(50);

            Assert.False(cts.Task.IsCompleted);
        }

        [Fact]
        public async Task AgentHandlesMessagesInOrder()
        {
            var parallelHandlers = 0;
            var random = new Random();
            Exception thrownException = null;
            var range = Enumerable.Range(0, 10);
            var tasks = range.Select(_ => new TaskCompletionSource<int>()).ToList();
            var agent = new AsyncAgent<int>(async (msg, ct) =>
            {
                if (1 != Interlocked.Increment(ref parallelHandlers))
                {
                    throw new Exception("parallelHandlers should be 1");
                }
                await Task.Delay(random.Next(5));
                Interlocked.Decrement(ref parallelHandlers);
                tasks[msg].SetResult(msg);
            });
            agent.Error += (ex) =>
            {
                thrownException = ex;
                foreach (var index in range)
                {
                    tasks[index].SetResult(-1);
                }
            };

            foreach (var msg in range)
            {
                agent.Send(msg);
            }
            await Task.WhenAll(tasks.Select(item => item.Task).ToArray());

            Assert.Null(thrownException);
        }
    }
}
