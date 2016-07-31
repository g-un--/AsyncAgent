using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAgentLib
{
    public class AsyncStateMachine<State> : IDisposable
    {
        private State _state;
        private AsyncAgent<Func<CancellationToken, Task<State>>> _processor;

        public AsyncStateMachine(State initialState)
        {
            _state = initialState;
            _processor = new AsyncAgent<Func<CancellationToken, Task<State>>>(async (message, cancellationToken) =>
            {
                _state = await message(cancellationToken);
            });
        }

        protected void Send<T>(T message, Func<State, T, CancellationToken, Task<State>> handler)
        {
            _processor.Send(cancellationToken =>
            {
                return handler(_state, message, cancellationToken);
            });
        }

        public void Dispose()
        {
            _processor.Dispose();
        }
    }
}
