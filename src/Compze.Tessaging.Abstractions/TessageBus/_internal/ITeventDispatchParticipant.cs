namespace Compze.Tessaging.TessageBus._internal;

///<summary>Code that takes part in the dispatch of a transaction's tevents (<see cref="ITransactionTeventDispatch"/>): it may refuse<br/>
/// the commit before any tevent is dispatched, and it prepares its own work once every tevent is, such as a taggregate store<br/>
/// preparing the write of the taggregates that the tevents' handlers changed.</summary>
interface ITeventDispatchParticipant
{
   ///<summary>Runs when the transaction prepares, before the first of its tevents is dispatched. Throwing rolls the transaction<br/>
   /// back before any participation handler has run. A participant that enlists during the dispatch isn't told.</summary>
   void BeforeTheTeventsAreDispatched();

   ///<summary>Runs once every tevent of the transaction is dispatched, the tevents that handlers published in turn included, and<br/>
   /// before any participant of the transaction votes. Throwing rolls the transaction back. No tevent may be published from here on.</summary>
   void OnceEveryTeventIsDispatched();
}
