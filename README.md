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
