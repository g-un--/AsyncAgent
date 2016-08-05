using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAgentLib.Reactive
{
    public class ReactiveAsyncAgent<TState, TMessage> : IDisposable
    {
        private int _disposed;
        private readonly BehaviorSubject<TState> _stateSubject;
        private readonly AsyncAgent<TState, TMessage> _asyncAgent;

        public IObservable<TState> Output { get; private set; }

        public ReactiveAsyncAgent(
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

            _stateSubject = new BehaviorSubject<TState>(initialState);
            _asyncAgent = new AsyncAgent<TState, TMessage>(
                initialState: initialState,
                messageHandler: async (state, msg, ct) =>
                {
                    TState newState = state;

                    newState = await messageHandler(state, msg, ct);

                    if (!ct.IsCancellationRequested && _disposed == 0)
                    {
                        try
                        {
                            _stateSubject.OnNext(newState);
                        }
                        finally { }
                    }

                    return newState;
                },
                errorHandler: async (ex, ct) =>
                {
                    bool shouldContinue = false;
                    shouldContinue = await errorHandler(ex, ct);

                    if (!shouldContinue && _disposed == 0)
                    {
                        try
                        {
                            _stateSubject.OnError(ex);
                        }
                        finally { }
                    }

                    return shouldContinue;
                });

            Output = _stateSubject.AsObservable();
        }

        public void Send(TMessage message)
        {
            if (0 == _disposed)
            {
                _asyncAgent.Send(message);
            }
        }

        public void Dispose()
        {
            if (0 == Interlocked.Exchange(ref _disposed, 1))
            {
                _asyncAgent.Dispose();
                try
                {
                    _stateSubject.OnCompleted();
                }
                finally
                {
                    _stateSubject.Dispose();
                }
            }
        }
    }
}
