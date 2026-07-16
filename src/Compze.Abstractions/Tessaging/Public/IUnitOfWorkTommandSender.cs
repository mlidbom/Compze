namespace Compze.Abstractions.Tessaging.Public;

///<summary>Sends exactly-once tommands within the caller's unit of work: the send joins the caller's ambient transaction, so a<br/>
/// rolled-back unit of work never leaks a tommand. Code outside any unit of work sends through<br/>
/// <see cref="IIndependentTommandSender"/>, the independent counterpart that gives each send its own.</summary>
public interface IUnitOfWorkTommandSender
{
   ///<summary>Sends <paramref name="tommand"/> when the caller's unit of work commits — the tommand joins the ambient<br/>
   /// transaction through the endpoint's outbox, exactly-once. The receiver executes its handler in a unit of work of its own.</summary>
   void Send(IExactlyOnceTommand tommand);
}
