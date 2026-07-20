using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Abstractions.TessageBus;

///<summary>Sends exactly-once tommands within the caller's unit of work: the send joins the caller's ambient transaction, so a<br/>
/// rolled-back unit of work never leaks a tommand. Code outside any unit of work sends through<br/>
/// <see cref="IIndependentTommandSender"/>, the independent counterpart that gives each send its own.</summary>
public interface IUnitOfWorkTommandSender
{
   ///<summary>Sends <paramref name="tommand"/> within the caller's unit of work, exactly-once. A tommand whose handler is in<br/>
   /// this endpoint's own roster executes inline in the caller's execution (the consistency law); any other joins the ambient<br/>
   /// transaction through the endpoint's outbox — a durable row written inside the caller's transaction, which is why the door<br/>
   /// is async: exactly-once kinds are async end to end — and its receiver executes its handler in a unit of work of its own.</summary>
   Task SendAsync(IExactlyOnceTommand tommand);
}
