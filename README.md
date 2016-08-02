# AsyncAgent
An agent which executes a Func&lt;TState, TMessage, CancellationToken, Task&lt;TState>> for each message from a ConcurrentQueue&lt;T>, accumulating an internal state.
```
var agent = new AsyncAgent<int, int>(
    initialState: 0,
    messageHandler: async (state, msg, ct) =>
    {
        //do stuff
        await Task.Delay(0, ct);
        return state + msg;
    },
    errorHandler: (ex) => Task.FromResult(true));

//it is safe to send messages from multiple threads
agent.Send(1);
```

[![Build status](https://ci.appveyor.com/api/projects/status/lwlmja34mnec0hi2/branch/master?svg=true)](https://ci.appveyor.com/project/g-un--/asyncagent/branch/master)
