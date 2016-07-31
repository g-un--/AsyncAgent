using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAgentLib
{
    public class AsyncStateMachine : IDisposable
    {
        private AsyncAgent<Func<CancellationToken, Task>> _processor;

        public AsyncStateMachine()
        {
            _processor = new AsyncAgent<Func<CancellationToken, Task>>(async (message, cancellationToken) =>
            {
                await message(cancellationToken);
            });
        }

        protected void Send<T>(T message, Func<T, CancellationToken, Task> handler)
        {
            _processor.Send(async (cancellationToken) =>
            {
                await handler(message, cancellationToken);
            });
        }

        public void Dispose()
        {
            _processor.Dispose();
        }
    }
}
