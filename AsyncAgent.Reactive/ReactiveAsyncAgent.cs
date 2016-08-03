using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAgentLib.Reactive
{
    public class ReactiveAsyncAgent<TState, TMessage> : IDisposable
    {
        private readonly IDisposable _disposables;

        public IObserver<TMessage> Input { get; private set; }
        public IObservable<TState> Output { get; private set; }

        public ReactiveAsyncAgent(
            TState initialState,
            Func<TState, TMessage, CancellationToken, Task<TState>> messageHandler,
            Func<Exception, Task<bool>> errorHandler)
        {
            BehaviorSubject<TState> stateSubject = new BehaviorSubject<TState>(initialState);
            var asyncAgent = new AsyncAgent<TState, TMessage>(
                initialState: initialState,
                messageHandler: async (state, msg, ct) =>
                {
                    var newState = await messageHandler(state, msg, ct);
                    await Task.Run(() => stateSubject.OnNext(newState));
                    return newState;
                },
                errorHandler: async ex =>
                {
                    bool shouldContinue = await errorHandler(ex);
                    if (!shouldContinue)
                    {
                        stateSubject.OnError(ex);
                    }
                    return shouldContinue;
                });

            var agentDisposable = Disposable.Create(() => asyncAgent.Dispose());
            var stateDisposable = Disposable.Create(() => stateSubject.Dispose());
           
            Input = Observer.Create<TMessage>(
                onNext: message => asyncAgent.Send(message),
                onError: ex =>
                {
                    stateSubject.OnError(ex);
                },
                onCompleted: () =>
                {
                    stateSubject.OnCompleted();
                });
            Output = stateSubject.AsObservable();

            _disposables = new CompositeDisposable(stateDisposable, agentDisposable);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
