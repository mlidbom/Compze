namespace Compze.Abstractions.Tessaging.Public;

///<summary>Dispatches tessages between processes.</summary>
public interface IServiceBusSession
{
    ///<summary>Sends a tommand if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
    void Send(IExactlyOnceTommand tommand);
}
