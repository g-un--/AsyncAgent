using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAgentLib
{
    public class AsyncAgent<TState, TMessage> : IDisposable
    {
        private readonly ConcurrentQueue<TMessage> _workItems;
        private readonly Func<Exception, CancellationToken, Task<bool>> _errorHandler;
        private readonly Func<TState, TMessage, CancellationToken, Task<TState>> _messageHandler;
        private readonly CancellationTokenSource _cts;

        private TState _currentState;
        private int _messagesCounter;
        private int _disposed;
        private volatile TaskCompletionSource<bool> _signal;

        public AsyncAgent(
            TState initialState,
            Func<TState, TMessage, CancellationToken, Task<TState>> messageHandler,
            Func<Exception, CancellationToken, Task<bool>> errorHandler)
        {
            _currentState = initialState;
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _workItems = new ConcurrentQueue<TMessage>();
            _signal = new TaskCompletionSource<bool>();
            _cts = new CancellationTokenSource();
            _messagesCounter = 0;
            _disposed = 0;
            ProcessItem(_cts.Token);
        }

        public void Send(TMessage message)
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
                return;

            if (1 == Interlocked.Increment(ref _messagesCounter))
            {
                _workItems.Enqueue(message);
                _signal.TrySetResult(true);
            }
            else
            {
                _workItems.Enqueue(message);
            }
        }

        private void ProcessItem(CancellationToken ct)
        {
            Task.Factory.StartNew(async state =>
            {
                if (_signal.Task.IsCompleted)
                    _signal = new TaskCompletionSource<bool>();

                var innerCt = (CancellationToken)state;

                if (Interlocked.CompareExchange(ref _messagesCounter, 0, 0) > 0)
                {
                    bool shouldContinue = true;

                    while (_workItems.TryDequeue(out TMessage item) && !innerCt.IsCancellationRequested)
                    {
                        try
                        {
                            _currentState = await _messageHandler(_currentState, item, innerCt);
                        }
                        catch (Exception ex)
                        {
                            shouldContinue = await _errorHandler(ex, innerCt);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _messagesCounter);
                        }

                        if (!shouldContinue)
                            break;
                    }

                    if (shouldContinue && !innerCt.IsCancellationRequested)
                    {
                        ProcessItem(innerCt);
                    }
                }
                else
                {
                    if (await _signal.Task && !innerCt.IsCancellationRequested)
                        ProcessItem(innerCt);
                }
            }, ct, ct);
        }

        public void Dispose()
        {
            if (0 == Interlocked.Exchange(ref _disposed, 1))
            {
                _cts.Cancel();
                _signal.TrySetResult(false);
                _cts.Dispose();
            }
        }
    }
}
