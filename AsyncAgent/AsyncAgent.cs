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
        private volatile TaskCompletionSource<bool> _signal;
        private int _messagesCounter;
        private int _disposed;
        private volatile Task _latestTask;

        public AsyncAgent(
            TState initialState,
            Func<TState, TMessage, CancellationToken, Task<TState>> messageHandler,
            Func<Exception, CancellationToken, Task<bool>> errorHandler)
        {
            if (initialState == null)
                throw new ArgumentNullException(nameof(initialState));

            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));

            if (errorHandler == null)
                throw new ArgumentNullException(nameof(errorHandler));

            _workItems = new ConcurrentQueue<TMessage>();
            _currentState = initialState;
            _messageHandler = messageHandler;
            _errorHandler = errorHandler;
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

            _latestTask = Task.Run(async () =>
            {
                if (Interlocked.CompareExchange(ref _messagesCounter, _messagesCounter, _messagesCounter) > 0)
                {
                    if (_signal.Task.IsCompleted)
                        _signal = new TaskCompletionSource<bool>();

                    TMessage item;
                    bool shouldContinue = true;

                    while (_workItems.TryDequeue(out item) && !ct.IsCancellationRequested)
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
                    SpinWait spin = new SpinWait();
                    var latestTask = _latestTask;
                    while (latestTask != _latestTask)
                    {
                        spin.SpinOnce();
                        latestTask = _latestTask;
                    }
                    latestTask.ContinueWith(_ => _cts.Dispose());
                }
            }
        }
    }
}
