using System.Transactions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Contracts;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>What the taggregate stores do together in one unit of work: there is one for each transaction that gets a taggregate<br/>
/// for update. It holds each store's <see cref="ITaggregateStoreSession"/>, and publishes the tevents their taggregates publish<br/>
/// through <see cref="IUnitOfWorkTeventPublisher"/>, which queues them in the transaction's one queue of tevents, in the order<br/>
/// published.<br/>
/// When the transaction prepares, before any participant has voted, the queue dispatches every tevent of the transaction, so<br/>
/// participation handlers run inside the transaction; what they publish in turn is dispatched in the same pass. Then each session<br/>
/// prepares its write, and when the transaction commits, each makes it.</summary>
///<remarks>The tevents are dispatched at the commit, not as they are published, so that nothing runs while a domain method of a<br/>
/// taggregate is on the stack: a participation handler that ran between two publishes of one domain method would see the<br/>
/// method half done. And a unit of work whose body throws dispatches nothing, so there is nothing to undo.<br/>
/// The queue belongs to the transaction, not to a store's session, so that a handler that changes a taggregate of one store while<br/>
/// another store's tevents are dispatched has its tevents dispatched too, before any store writes: the sessions prepare once the<br/>
/// queue has dispatched every tevent, as an <see cref="ITeventDispatchParticipant"/> of it.</remarks>
sealed class TaggregateUnitOfWork : VolatileTransactionParticipant, ITeventDispatchParticipant
{
   readonly IUnitOfWorkTeventPublisher _publisher;
   readonly Dictionary<ITaggregateStore, ITaggregateStoreSession> _sessions = new();
   bool _hasPrepared;

   ///<summary>Begins the taggregate unit of work of the ambient transaction, which <paramref name="unitOfWork"/> began.<br/>
   /// <paramref name="ended"/> runs when the transaction has completed.</summary>
   ///<remarks>It votes in the transaction's second phase, after the dispatch of the transaction's tevents, in the first, has had<br/>
   /// every session prepare.</remarks>
   internal TaggregateUnitOfWork(IUnitOfWorkResolver unitOfWork, Action ended) : base(EnlistmentOptions.None)
   {
      _publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
      var transaction = Transaction.Current._assert().NotNull();
      EnsureEnlistedInAnyAmbientTransaction();
      unitOfWork.Resolve<ITransactionTeventDispatch>().Enlist(this);

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

   ///<summary>Publishes a tevent that a taggregate of this unit of work published. The publisher queues it, to be dispatched when<br/>
   /// the transaction prepares: nothing is dispatched now, while the domain method that published it may still be on the stack.</summary>
   internal void Publish(ITaggregateTevent<ITaggregateTevent> tevent)
   {
      AssertHasNotPrepared();
      //PublishAsync serves every kind of tevent, the exactly-once kind included, and completes at once: it only queues the tevent.
      _publisher.PublishAsync(tevent).GetAwaiter().GetResult();
   }

   void AssertHasNotPrepared() =>
      State.Assert(!_hasPrepared, () => "This unit of work has prepared to commit its taggregates, so it changes no taggregate any more.");

   void ITeventDispatchParticipant.BeforeTheTeventsAreDispatched() => AssertNoPublishHasFailed();

   void ITeventDispatchParticipant.OnceEveryTeventIsDispatched()
   {
      AssertNoPublishHasFailed();
      foreach(var session in _sessions.Values) session.Prepare();
      _hasPrepared = true;
   }

   protected override void OnPrepare() =>
      Invariant.Assert(_hasPrepared, () => "A taggregate unit of work votes once the dispatch of its transaction's tevents has had its sessions prepare, and they haven't.");

   void AssertNoPublishHasFailed()
   {
      foreach(var session in _sessions.Values) session.AssertNoPublishHasFailed();
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
