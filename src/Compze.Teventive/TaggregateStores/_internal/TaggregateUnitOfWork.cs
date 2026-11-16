using System.Transactions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Contracts;
using Compze.Teventive.TaggregateStores._private;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>What the taggregate stores do together in one unit of work: there is one for each transaction that gets a taggregate<br/>
/// for update. It holds each store's <see cref="ITaggregateStoreSession"/>, and collects the tevents their taggregates publish, in<br/>
/// the order published.<br/>
/// When the transaction prepares, before any participant has voted, it dispatches them through<br/>
/// <see cref="IUnitOfWorkTeventPublisher"/>, so participation handlers run inside the transaction; what they publish in turn is<br/>
/// dispatched in the same pass. Then each session prepares its write, and when the transaction commits, each makes it.</summary>
///<remarks>The tevents are dispatched at the commit, not as they are published, so that nothing runs while a domain method of a<br/>
/// taggregate is on the stack: a participation handler that ran between two publishes of one domain method would see the<br/>
/// method half done. And a unit of work whose body throws dispatches nothing, so there is nothing to undo.<br/>
/// The queue belongs to the unit of work, not to a store's session, so that a handler that changes a taggregate of one store while<br/>
/// another store's tevents are dispatched has its tevents dispatched too, before any store writes.</remarks>
sealed class TaggregateUnitOfWork : VolatileTransactionParticipant
{
   ///<summary>How many rounds of tevents caused by tevents one commit dispatches before it gives up: two handlers that cause<br/>
   /// each other's tevents would otherwise hang the commit.</summary>
   const int MaximumDispatchRounds = 100;

   readonly IUnitOfWorkResolver _unitOfWork;
   readonly Dictionary<ITaggregateStore, ITaggregateStoreSession> _sessions = new();
   readonly Queue<ITaggregateTevent<ITaggregateTevent>> _teventsToDispatch = new();
   bool _hasPrepared;

   ///<summary>Begins the taggregate unit of work of the ambient transaction, which <paramref name="unitOfWork"/> began.<br/>
   /// <paramref name="ended"/> runs when the transaction has completed.</summary>
   internal TaggregateUnitOfWork(IUnitOfWorkResolver unitOfWork, Action ended) : base(EnlistmentOptions.EnlistDuringPrepareRequired)
   {
      _unitOfWork = unitOfWork;
      var transaction = Transaction.Current._assert().NotNull();
      EnsureEnlistedInAnyAmbientTransaction();

      //Completion comes after the commit or rollback notification, so the next unit of work to take a lock finds the committed instance.
      transaction.TransactionCompleted += (_, _) =>
      {
         ended();
         foreach(var session in _sessions.Values) session.Complete();
      };
   }

   ///<summary>The session of <paramref name="store"/> in this unit of work, if the unit of work has needed the store yet.</summary>
   internal ITaggregateStoreSession? ExistingSessionOf(ITaggregateStore store) => _sessions.GetValueOrDefault(store);

   ///<summary>The session of <paramref name="store"/> in this unit of work, begun now if the unit of work hasn't needed the store yet.</summary>
   internal ITaggregateStoreSession SessionOf(ITaggregateStore store)
   {
      AssertHasNotPrepared();
      if(_sessions.TryGetValue(store, out var session)) return session;
      session = store.NewSession(this);
      _sessions.Add(store, session);
      return session;
   }

   ///<summary>Collects a tevent that a taggregate of this unit of work published, to be dispatched when the transaction prepares.<br/>
   /// Dispatches nothing now: the domain method that published it may still be on the stack.</summary>
   internal void Collect(ITaggregateTevent<ITaggregateTevent> tevent)
   {
      AssertHasNotPrepared();
      _teventsToDispatch.Enqueue(tevent);
   }

   void AssertHasNotPrepared() =>
      State.Assert(!_hasPrepared, () => "This unit of work has prepared to commit its taggregates, so it changes no taggregate any more.");

   protected override void OnPrepare()
   {
      AssertNoPublishHasFailed();
      DispatchTheCollectedTevents();
      AssertNoPublishHasFailed();
      foreach(var session in _sessions.Values) session.Prepare();
      _hasPrepared = true;
   }

   void AssertNoPublishHasFailed()
   {
      foreach(var session in _sessions.Values) session.AssertNoPublishHasFailed();
   }

   ///<summary>Dispatches in rounds: the tevents collected when the round began, in the order published, during which handlers may<br/>
   /// publish more, which the next round dispatches. So cause comes before consequence.</summary>
   void DispatchTheCollectedTevents()
   {
      var publisher = _unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
      for(var round = 1; _teventsToDispatch.Count > 0; round++)
      {
         State.Assert(round <= MaximumDispatchRounds,
                      () => $"After {MaximumDispatchRounds} rounds of tevents caused by tevents, this unit of work still has tevents to dispatch, so handlers seem to cause each other's tevents without end. The tevents of the last round: {string.Join(", ", _teventsToDispatch.Select(it => it.Tevent.GetType().FullName).Distinct())}.");
         var thisRound = _teventsToDispatch.ToList();
         _teventsToDispatch.Clear();
         foreach(var tevent in thisRound) Dispatch(tevent, publisher);
      }
   }

   static void Dispatch(ITaggregateTevent<ITaggregateTevent> tevent, IUnitOfWorkTeventPublisher publisher)
   {
      try
      {
         if(tevent.Tevent is IExactlyOnceTevent)
         {
            //Exactly-once tevents are published only asynchronously, and the transaction prepares synchronously: the one deliberate
            //bridge, as in the tevent store.
            publisher.PublishAsync(tevent).GetAwaiter().GetResult();
         } else
         {
            publisher.Publish(tevent);
         }
      }
      catch(Exception exception)
      {
         throw new TaggregateTeventDispatchFailedException(tevent, exception);
      }
   }

   protected override void OnCommit()
   {
      foreach(var session in _sessions.Values) session.Commit();
   }

   protected override void OnRollback()
   {
      foreach(var session in _sessions.Values) session.Rollback();
   }
}
