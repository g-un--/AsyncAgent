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
        private TaskCompletionSource<bool> _signal;
        private int _signalSync;
        private int _writers;
        private int _disposed;

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
            _signalSync = 0;
            _disposed = 0;
            _writers = 0;
            ProcessItem();
        }

        public void Send(TMessage message)
        {
            if (Volatile.Read(ref _disposed) == 0)
            {
                Interlocked.Increment(ref _writers);

                _workItems.Enqueue(message);

                if (0 == Interlocked.Exchange(ref _signalSync, 1))
                {
                    _signal.TrySetResult(true);
                }

                Interlocked.Decrement(ref _writers);
            }
        }

        private void ProcessItem()
        {
            Task.Run(async () =>
            {
                if (1 == Interlocked.CompareExchange(ref _signalSync, 1, 1))
                {
                    if(_signal.Task.IsCompleted)
                        _signal = new TaskCompletionSource<bool>();

                    TMessage item;
                    bool shouldContinue = true;

                    while (!_cts.IsCancellationRequested && _workItems.TryDequeue(out item))
                    {
                        try
                        {
                            _currentState = await _messageHandler(_currentState, item, _cts.Token);
                        }
                        catch (Exception ex)
                        {
                            shouldContinue = await _errorHandler(ex, _cts.Token);
                            if (!shouldContinue)
                                break;
                        }
                    }

                    if (shouldContinue)
                    {
                        Interlocked.Exchange(ref _signalSync, _writers > 0 ? 1 : 0);
                        ProcessItem();
                    }
                }
                else
                {
                    Interlocked.Exchange(ref _signalSync, _writers > 0 ? 1 : 0);

                    if (Volatile.Read(ref _signalSync) == 0)
                        await _signal.Task;

                    ProcessItem();
                }
            }, _cts.Token);
        }

        public void Dispose()
        {
            if (0 == Interlocked.Exchange(ref _disposed, 1))
            {
                _cts.Cancel();
                _signal.TrySetResult(false);
            }
        }
    }
}
