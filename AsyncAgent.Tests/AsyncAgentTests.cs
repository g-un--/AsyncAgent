using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncAgentLib.Tests
{
    public class AsyncAgentTests
    {
        [Fact]
        public void AgentThrowsArgumentNullExceptionForInitialState()
        {
            ArgumentNullException thrownException = null;

            try
            {
                new AsyncAgent<string, string>(
                    initialState: null, 
                    messageHandler: (state, msg, ct) => Task.FromResult(state),
                    errorHandler: (ex, ct) => Task.FromResult(true));
            }
            catch (ArgumentNullException ex)
            {
                thrownException = ex;
            }

            Assert.NotNull(thrownException);
            Assert.True(string.CompareOrdinal(thrownException.ParamName, "initialState") == 0);
        }

        [Fact]
        public void AgentThrowsArgumentNullExceptionForMessageHandler()
        {
            ArgumentNullException thrownException = null;

            try
            {
                new AsyncAgent<string, string>(
                    initialState: string.Empty,
                    messageHandler: null,
                    errorHandler: (ex, ct) => Task.FromResult(true));
            }
            catch (ArgumentNullException ex)
            {
                thrownException = ex;
            }

            Assert.NotNull(thrownException);
            Assert.True(string.CompareOrdinal(thrownException.ParamName, "messageHandler") == 0);
        }

        [Fact]
        public void AgentThrowsArgumentNullExceptionForErrorHandler()
        {
            ArgumentNullException thrownException = null;

            try
            {
                new AsyncAgent<string, string>(
                    initialState: string.Empty,
                    messageHandler: (state, msg, ct) => Task.FromResult(state),
                    errorHandler: null);
            }
            catch (ArgumentNullException ex)
            {
                thrownException = ex;
            }

            Assert.NotNull(thrownException);
            Assert.True(string.CompareOrdinal(thrownException.ParamName, "errorHandler") == 0);
        }

        [Fact]
        public async Task AgentCanHandleMessage()
        {
            var tcs = new TaskCompletionSource<string>();
            var message = "test";
            var agent = new AsyncAgent<string, string>(
                string.Empty,
                async (state, msg, ct) =>
                {
                    await Task.Delay(0, ct);
                    tcs.SetResult(msg);
                    return state;
                },
                (ex, ct) => Task.FromResult(true));

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
            var agent = new AsyncAgent<string, string>(
                initialState: string.Empty,
                messageHandler: async (state, msg, ct) =>
                {
                    await Task.Delay(0, ct);
                    throw exception;
                },
                errorHandler: (ex, ct) =>
                {
                    tcs.SetResult(ex);
                    return Task.FromResult(true);
                });

            agent.Send(message);
            var triggeredException = await tcs.Task;

            Assert.Equal(exception, triggeredException);
        }

        [Fact]
        public async Task AgentDoesNotHandleMessagesAfterDispose()
        {
            var tcs = new TaskCompletionSource<int>();
            var agent = new AsyncAgent<int, int>(
                initialState: 0,
                messageHandler: async (state, msg, ct) =>
                {
                    await Task.Delay(0, ct);
                    tcs.SetResult(msg);
                    return state;
                },
                errorHandler: (ex, ct) => Task.FromResult(true));

            agent.Dispose();
            agent.Send(1);
            await Task.Delay(50);

            Assert.False(tcs.Task.IsCompleted);
        }

        [Fact]
        public async Task AgentHandlesMessagesInOrder()
        {
            var parallelHandlers = 0;
            var random = new Random();
            Exception thrownException = null;
            var range = Enumerable.Range(0, 10);
            var tasks = range.Select(_ => new TaskCompletionSource<int>()).ToList();
            var agent = new AsyncAgent<int, int>(
                initialState: 0,
                messageHandler: async (state, msg, ct) =>
                {
                    if (1 != Interlocked.Increment(ref parallelHandlers))
                    {
                        throw new Exception("parallelHandlers should be 1");
                    }

                    await Task.Delay(random.Next(5));

                    if(0 != Interlocked.Decrement(ref parallelHandlers))
                    {
                        throw new Exception("parrallelHandlers should be 0");
                    }

                    tasks[msg].SetResult(msg);

                    return state;
                },
                errorHandler: (ex, ct) =>
                {
                    thrownException = ex;
                    return Task.FromResult(true);
                });

            foreach (var msg in range)
            {
                agent.Send(msg);
            }
            await Task.WhenAll(tasks.Select(item => item.Task).ToArray());
            
            Assert.Null(thrownException);
        }
    }
}
