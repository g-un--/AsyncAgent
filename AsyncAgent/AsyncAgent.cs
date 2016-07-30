using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAgentLib
{
    public class AsyncAgent<T> : IDisposable
    {
        public event Action<Exception> Error;

        private ConcurrentQueue<T> _workItems;
        private Func<T, CancellationToken, Task> _handler;
        private CancellationTokenSource _cts;
        private TaskCompletionSource<bool> _signal;
        private int _signalSync;
        private int _writers;
        private int _disposed;

        public AsyncAgent(Func<T, CancellationToken, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            _workItems = new ConcurrentQueue<T>();
            _handler = handler;
            _signal = new TaskCompletionSource<bool>();
            _cts = new CancellationTokenSource();
            _signalSync = 0;
            _disposed = 0;
            _writers = 0;
            ProcessItem();
        }

        public void Send(T message)
        {
            if (0 == Interlocked.CompareExchange(ref _disposed, 0, 0))
            {
                Interlocked.Increment(ref _writers);

                _workItems.Enqueue(message);

                if (0 == Interlocked.Exchange(ref _signalSync, 1))
                {
                    _signal.SetResult(true);
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

                    T item;
                    while (!_cts.IsCancellationRequested && _workItems.TryDequeue(out item))
                    {
                        try
                        {
                            await _handler(item, _cts.Token);
                        }
                        catch (Exception ex)
                        {
                            Error?.Invoke(ex);
                        }
                    }

                    Interlocked.Exchange(ref _signalSync, _writers > 0 ? 1 : 0);
                    ProcessItem();
                }
                else
                {
                    Interlocked.Exchange(ref _signalSync, _writers > 0 ? 1 : 0);

                    if (_signalSync == 0)
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
