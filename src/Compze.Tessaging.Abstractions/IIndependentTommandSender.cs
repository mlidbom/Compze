using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Abstractions;

///<summary>Sends exactly-once tommands from code that runs outside any unit of work — application code with no ambient scope<br/>
/// or transaction. Each send runs as its own independent unit of work, committed when the call returns: the tommand is then<br/>
/// durably in the endpoint's outbox and on its way. The independent counterpart of <see cref="IUnitOfWorkTommandSender"/>,<br/>
/// which sends within the caller's unit of work.</summary>
///<remarks>An <see cref="IExactlyOnceTommand"/> must be sent transactionally — the outbox row and the sender's other effects<br/>
/// commit atomically — so a caller without a unit of work needs one begun for it; that is exactly what this sender does.<br/>
/// Independence is asserted, not assumed: sending from within an ambient transaction throws, because the send would silently<br/>
/// join that transaction instead of standing alone. Inside a unit of work, send through<br/>
/// <see cref="IUnitOfWorkTommandSender"/>, which deliberately joins it. Resolvable from the root: a singleton, so plain<br/>
/// application classes take it as an ordinary constructor dependency.</remarks>
public interface IIndependentTommandSender
{
   ///<summary>Sends <paramref name="tommand"/> as its own unit of work — see <see cref="IUnitOfWorkTommandSender.SendAsync"/>.<br/>
   /// The unit of work commits when the awaited send completes; the tommand is then durably on its way, exactly-once.</summary>
   Task SendAsync(IExactlyOnceTommand tommand);
}
