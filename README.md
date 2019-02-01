# IBM.Socket-IO

Offers the ability to connect to Socket.IO servers using C#.  Built using .NET
Standard 2.0.

## Example

The following C# code works for .NET 4.7.2:

```this.mediator = new SocketMediator(this.endpointUrl);
var task = this.mediator.InitConnection(new SampleHttpClientFactory(), new ClientSocketFactory());
var awaiter = task.GetAwaiter();

awaiter.OnCompleted(() => 
{
    if(task.IsFaulted)
    {
      this.ShowError("Could not connect to endpointUrl!");
    }
    else
    {
        this.ShowMessage("Connected Successfully");
    }
});```

## Known Limitations
Currently connecting to Socket.IO rooms is the only supported connection method.

To connect to a Socket.IO server, use a URL such as:

`http://localhost:3000/myroom`

So far referencing this library in a .NET 4.7 project works but not in ASP.NET
Core.  When connecting inside an ASP.NET Core project, the final handshake 
hangs.  However in .NET 4.7 handshake succeeds and `emit()` works just fine.

We are open to contributions and fixes for why this library fails in ASP.NET
Core!

## Support

For ideas and/or help, ping us on Slack: 
https://ibm-security.slack.com/messages/CFELND99Q
