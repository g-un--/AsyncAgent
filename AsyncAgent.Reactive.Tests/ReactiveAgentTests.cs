using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncAgentLib.Reactive.Tests
{
    public class ReactiveAgentTests
    {
        [Fact]
        public void AgentThrowsArgumentNullExceptionForInitialState()
        {
            ArgumentNullException thrownException = null;

            try
            {
                new ReactiveAsyncAgent<string, string>(
                    initialState: null,
                    messageHandler: (state, msg, ct) => Task.FromResult(state),
                    errorHandler: ex => Task.FromResult(true));
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
                new ReactiveAsyncAgent<string, string>(
                    initialState: string.Empty,
                    messageHandler: null,
                    errorHandler: ex => Task.FromResult(true));
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
                new ReactiveAsyncAgent<string, string>(
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
            var message = "test";
            var tcs = new TaskCompletionSource<string>();

            var agent = new ReactiveAsyncAgent<string, string>(
                initialState: string.Empty,
                messageHandler: (state, msg, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult(msg);
                },
                errorHandler: (ex) => Task.FromResult(true));

            agent.Output.Skip(1).Take(1).Do(msg => tcs.SetResult(msg)).Subscribe();
            agent.Input.OnNext(message);
            var receivedMessage = await tcs.Task;

            Assert.Equal(message, receivedMessage);
        }

        [Fact]
        public async Task AgentCallsOnErrorForUnhandledException()
        {
            var exception = new Exception();
            var tcs = new TaskCompletionSource<Exception>();

            var agent = new ReactiveAsyncAgent<string, string>(
                initialState: string.Empty,
                messageHandler: (state, msg, ct) =>
                {
                    throw exception;
                },
                errorHandler: (ex) => Task.FromResult(false));

            agent.Output.Subscribe(_ => { }, ex => tcs.SetResult(ex));
            agent.Input.OnNext(string.Empty);
            var receivedException = await tcs.Task;

            Assert.Equal(exception, receivedException);
        }

        [Fact]
        public async Task AgentSignalsCompleted()
        {
            var tcs = new TaskCompletionSource<bool>();

            var agent = new ReactiveAsyncAgent<string, string>(
                initialState: string.Empty,
                messageHandler: (state, msg, ct) =>
                {
                    return Task.FromResult(msg); 
                },
                errorHandler: (ex) => Task.FromResult(true));

            agent.Output.Subscribe(_ => { }, () => tcs.SetResult(true));
            agent.Input.OnCompleted();
            var isCompleted = await tcs.Task;
            Assert.True(isCompleted);
        }

        [Fact]
        public async Task AgentReactsToMultipleMessages()
        {
            var tcs = new TaskCompletionSource<bool>();

            var agent = new ReactiveAsyncAgent<int, int>(
                initialState: 0,
                messageHandler: (state, msg, ct) =>
                {
                    return Task.FromResult(state + 1);
                },
                errorHandler: (ex) => Task.FromResult(true));

            var allMessagesTask = agent
                .Output.Take(1001).RunAsync(CancellationToken.None);

            foreach(var msg in Enumerable.Repeat(1, 1000))
            {
                agent.Input.OnNext(1000);
            }

            var latestState = await allMessagesTask;
            Assert.Equal(1000, latestState);
        }

        [Fact]
        public void AgentCanHandleMultipleDisposeCalls()
        {
            Exception exception = null;

            var tcs = new TaskCompletionSource<bool>();
            var agent = new ReactiveAsyncAgent<int, int>(
                initialState: 0,
                messageHandler: (state, msg, ct) =>
                {
                    return Task.FromResult(state + 1);
                },
                errorHandler: (ex) => Task.FromResult(true));

            try
            {
                agent.Dispose();
                agent.Dispose();
                agent.Input.OnNext(1);
            }
            catch(Exception ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        }
    }
}
