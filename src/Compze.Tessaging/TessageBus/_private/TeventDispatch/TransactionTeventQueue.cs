using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageBus._internal;

namespace Compze.Tessaging.TessageBus._private.TeventDispatch;

///<summary>The tevents published in one transaction, through <see cref="IUnitOfWorkTeventPublisher"/>, waiting for the transaction<br/>
/// to prepare: there is one for each transaction that publishes, whichever unit of work in it published, in the order published.<br/>
/// When the transaction prepares, before any participant has voted, it dispatches them: each to its participation handlers, in the<br/>
/// scope of the unit of work that began the transaction, then to its remote delivery leg. What the handlers publish is dispatched<br/>
/// in the same pass. Then it tells its <see cref="ITeventDispatchParticipant"/>s, such as the taggregate stores, which prepare<br/>
/// their writes.</summary>
///<remarks>The tevents are dispatched at the commit, not as they are published, so that nothing runs while the code that published<br/>
/// is still on the stack, such as a domain method of a taggregate, which would be seen half done; and so that every tevent of a<br/>
/// unit of work is dispatched in one order, whoever published it. A unit of work whose body throws dispatches nothing.<br/>
/// An exactly-once tevent's outbox rows are written by its remote delivery, in this transaction, in the order of this queue:<br/>
/// the business data and the rows commit or roll back together, in the order published.</remarks>
sealed class TransactionTeventQueue : VolatileTransactionParticipant
{
   ///<summary>How many rounds of tevents caused by tevents one commit dispatches before it gives up: two handlers that cause<br/>
   /// each other's tevents would otherwise hang the commit.</summary>
   const int MaximumDispatchRounds = 100;

   readonly TessageHandlerExecutor _executor;
   readonly IUnitOfWorkResolver _unitOfWorkThatBeganTheTransaction;
   readonly Queue<QueuedTevent> _teventsToDispatch = new();
   readonly List<ITeventDispatchParticipant> _participants = [];
   bool _everyTeventIsDispatched;

   ///<summary>Begins the queue of the ambient transaction, which <paramref name="unitOfWorkThatBeganTheTransaction"/> began: the<br/>
   /// participation handlers run in its scope, which lives until the transaction has completed.</summary>
   internal TransactionTeventQueue(TessageHandlerExecutor executor, IUnitOfWorkResolver unitOfWorkThatBeganTheTransaction) : base(EnlistmentOptions.EnlistDuringPrepareRequired)
   {
      _executor = executor;
      _unitOfWorkThatBeganTheTransaction = unitOfWorkThatBeganTheTransaction;
      EnsureEnlistedInAnyAmbientTransaction();
   }

   ///<summary>Queues <paramref name="tevent"/>, to be dispatched when the transaction prepares, after the tevents queued before it.</summary>
   internal void Enqueue(QueuedTevent tevent)
   {
      AssertNotEveryTeventIsDispatched();
      _teventsToDispatch.Enqueue(tevent);
   }

   ///<summary>As <see cref="ITransactionTeventDispatch.Enlist"/>, for this queue's transaction.</summary>
   internal void Enlist(ITeventDispatchParticipant participant)
   {
      AssertNotEveryTeventIsDispatched();
      _participants.Add(participant);
   }

   void AssertNotEveryTeventIsDispatched() =>
      State.Assert(!_everyTeventIsDispatched, () => "This transaction has dispatched every tevent published in it, and is voting on its commit, so nothing more can be published in it.");

   protected override void OnPrepare()
   {
      foreach(var participant in _participants.ToList()) participant.BeforeTheTeventsAreDispatched();
      DispatchInRounds();
      _everyTeventIsDispatched = true;
      foreach(var participant in _participants) participant.OnceEveryTeventIsDispatched();
   }

   ///<summary>Dispatches in rounds: the tevents queued when the round began, in the order published, during which handlers may<br/>
   /// publish more, which the next round dispatches. So cause comes before consequence.</summary>
   void DispatchInRounds()
   {
      for(var round = 1; _teventsToDispatch.Count > 0; round++)
      {
         State.Assert(round <= MaximumDispatchRounds,
                      () => $"After {MaximumDispatchRounds} rounds of tevents caused by tevents, this unit of work still has tevents to dispatch, so handlers seem to cause each other's tevents without end. The tevents of the last round: {string.Join(", ", _teventsToDispatch.Select(it => it.WrappedTevent.Tevent.GetType().FullName).Distinct())}.");
         var thisRound = _teventsToDispatch.ToList();
         _teventsToDispatch.Clear();
         foreach(var tevent in thisRound) Dispatch(tevent);
      }
   }

   void Dispatch(QueuedTevent tevent)
   {
      try
      {
         //The transaction prepares synchronously, and handlers and the exactly-once delivery leg are async: the one deliberate bridge.
         tevent.DispatchAsync(_executor, _unitOfWorkThatBeganTheTransaction).GetAwaiter().GetResult();
      }
      catch(Exception exception)
      {
         throw new TeventDispatchFailedException(tevent.WrappedTevent, exception);
      }
   }

   protected override void OnCommit() {}

   protected override void OnRollback() {}
}
