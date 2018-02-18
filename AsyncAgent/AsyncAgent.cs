using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAgentLib
{
    public class AsyncAgent<TState, TMessage> : IDisposable
    {
        private ConcurrentQueue<TMessage> _workItems;
        private Func<Exception, CancellationToken, Task<bool>> _errorHandler;
        private TState _currentState;
        private Func<TState, TMessage, CancellationToken, Task<TState>> _messageHandler;
        private CancellationTokenSource _cts;
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
            if (ct.IsCancellationRequested)
                return;

            Task.Run(async () =>
            {
                if (_signal.Task.IsCompleted)
                    _signal = new TaskCompletionSource<bool>();

                if (Interlocked.CompareExchange(ref _messagesCounter, 0, 0) > 0)
                {
                    bool shouldContinue = true;

                    while (_workItems.TryDequeue(out TMessage item) && !ct.IsCancellationRequested)
                    {
                        try
                        {
                            _currentState = await _messageHandler(_currentState, item, ct);
                        }
                        catch (Exception ex)
                        {
                            shouldContinue = await _errorHandler(ex, ct);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _messagesCounter);
                        }

                        if (!shouldContinue)
                            break;
                    }

                    if (shouldContinue && !ct.IsCancellationRequested)
                    {
                        ProcessItem(ct);
                    }
                }
                else
                {
                    if (await _signal.Task && !ct.IsCancellationRequested)
                        ProcessItem(ct);
                }
            }, ct);
        }

        public void Dispose()
        {
            if (0 == Interlocked.Exchange(ref _disposed, 1))
            {
                try { }
                finally
                {
                    _cts.Cancel();
                    _signal.TrySetResult(false);
                    _cts.Dispose();
                }
            }
        }
    }
}
