# AsyncAgent
An agent which executes a Func&lt;T, CancellationToken, Task> for each message from a ConcurrentQueue&lt;T>
```
var agent = new AsyncAgent<string>(async (msg, cancellationToken) =>
{
    //do stuff
    await Task.Delay(0);
}
agent.Error += (ex) => log(ex);

//it is safe to send messages from multiple threads
agent.Send("Hello world!");

//when finished
agent.Dispose();
```

[![Build status](https://ci.appveyor.com/api/projects/status/lwlmja34mnec0hi2/branch/master?svg=true)](https://ci.appveyor.com/project/g-un--/asyncagent/branch/master)

