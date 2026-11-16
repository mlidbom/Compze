namespace Compze.Tessaging.TessageBus._internal;

///<summary>The dispatch of a transaction's tevents: every tevent published through <see cref="IUnitOfWorkTeventPublisher"/> in a<br/>
/// transaction is dispatched when the transaction prepares to commit, in the order published. Code that keeps work of its own<br/>
/// for a unit of work, such as the taggregate stores, takes part in that dispatch through this interface.</summary>
interface ITransactionTeventDispatch
{
   ///<summary>Has <paramref name="participant"/> take part in the dispatch of the ambient transaction's tevents. Fails once every<br/>
   /// tevent of the transaction is dispatched.</summary>
   void Enlist(ITeventDispatchParticipant participant);
}
