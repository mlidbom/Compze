using System;

namespace Compze.Abstractions.Tessaging.Public;

///<summary>Dispatches messages between processes.</summary>
public interface IServiceBusSession
{
    ///<summary>Sends a command if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
    void Send(IExactlyOnceCommand command);

    ///<summary>Schedules a command to be sent later if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
    void ScheduleSend(DateTime sendAt, IExactlyOnceCommand command);
}
