﻿using System;
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
        public void AgentThrowsArgumentNullExceptionForMessageHandler()
        {
            ArgumentNullException thrownException = null;

            try
            {
                new ReactiveAsyncAgent<string, string>(
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
                errorHandler: (ex, ct) => Task.FromResult(true));

            agent.State.Skip(1).Take(1).Do(msg => tcs.SetResult(msg)).Subscribe();
            agent.Send(message);
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
                errorHandler: (ex, ct) => Task.FromResult(false));

            agent.State.Subscribe(_ => { }, ex => tcs.SetResult(ex));
            agent.Send(string.Empty);
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
                errorHandler: (ex, ct) => Task.FromResult(true));

            agent.State.Subscribe(_ => { }, () => tcs.SetResult(true));
            agent.Dispose();
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
                errorHandler: (ex, ct) => Task.FromResult(true));

            var allMessagesTask = agent
                .State.Take(1001).RunAsync(CancellationToken.None);

            foreach(var msg in Enumerable.Repeat(1, 1000))
            {
                agent.Send(1000);
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
                errorHandler: (ex, ct) => Task.FromResult(true));

            try
            {
                agent.Dispose();
                agent.Dispose();
                agent.Send(1);
            }
            catch(Exception ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        }
    }
}
