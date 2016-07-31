using AsyncAgentLib;
using System;
using System.Threading.Tasks;

namespace Playground
{
    public enum LockState { Locked, Unlocked }

    public class Gate<T> : AsyncStateMachine<LockState>
    {
        private Func<T, Task> _handler;

        public Gate(LockState initialState, Func<T, Task> handler) : base(initialState)
        {
            _handler = handler;
        }

        public void Send(T message)
        {
            Send(message, async (state, messageToPrint, cancellationToken) =>
            {
                if (state == LockState.Unlocked)
                {
                    await _handler(message);
                }

                return state;
            });
        }

        public void Unlock()
        {
            Send(LockState.Unlocked, async (state, message, ct) =>
            {
                await Task.Delay(0);
                return message;
            });
        }

        public void Lock()
        {
            Send(LockState.Locked, async (state, message, ct) =>
            {
                await Task.Delay(0);
                return message;
            });
        }
    }
}
