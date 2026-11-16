using System.Transactions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Contracts;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>What the taggregate stores do together in one unit of work: there is one for each transaction that gets a taggregate<br/>
/// for update. It holds each store's <see cref="ITaggregateStoreSession"/>, and dispatches the tevents their taggregates publish<br/>
/// through <see cref="IUnitOfWorkTeventPublisher"/> as each publish cascade ends, so participation handlers run synchronously,<br/>
/// inside the transaction, and what they publish is dispatched as it's published. When the transaction prepares, each session<br/>
/// prepares its write, and when the transaction commits, each makes it.</summary>
///<remarks>A tevent reaches the handlers once its taggregate is at rest: its own cascade has ended and its invariants hold. A tevent<br/>
/// a handler publishes on the same taggregate goes out after those before it, in the order published.</remarks>
sealed class TaggregateUnitOfWork : VolatileTransactionParticipant
{
   readonly IUnitOfWorkResolver _unitOfWork;
   readonly Dictionary<ITaggregateStore, ITaggregateStoreSession> _sessions = new();
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

   ///<summary>Dispatches a tevent that a taggregate of this unit of work published to the participation handlers, synchronously.</summary>
   internal void Dispatch(ITaggregateTevent<ITaggregateTevent> tevent)
   {
      AssertHasNotPrepared();
      var publisher = _unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
      if(tevent.Tevent is IExactlyOnceTevent)
      {
         //Exactly-once tevents are published only asynchronously, and a taggregate publishes synchronously: the one deliberate
         //bridge, as in the tevent store.
         publisher.PublishAsync(tevent).GetAwaiter().GetResult();
      } else
      {
         publisher.Publish(tevent);
      }
   }

   void AssertHasNotPrepared() =>
      State.Assert(!_hasPrepared, () => "This unit of work has prepared to commit its taggregates, so it changes no taggregate any more.");

   protected override void OnPrepare()
   {
      AssertNoPublishHasFailed();
      foreach(var session in _sessions.Values) session.Prepare();
      _hasPrepared = true;
   }

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
