using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Teventive._internal;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore._private;

///<summary>The tevent store's part of one unit of work: the taggregates it got for update or added, each built from its history once<br/>
/// for the unit of work. Each tevent they publish is written to the tevent store as its taggregate hands it out, inside the unit of<br/>
/// work's transaction, and then dispatched. The database commits or rolls back the writes with the transaction, so this session has<br/>
/// nothing to prepare or commit itself. A taggregate that publishes an <see cref="ITaggregateDeletedTevent"/> is deleted: its history,<br/>
/// which ends with that tevent, is kept, and the unit of work no longer finds it.</summary>
sealed class TeventStoreSession : ITaggregateStoreSession
{
   readonly TaggregateTeventStore _store;
   readonly TaggregateUnitOfWork _unitOfWork;
   readonly Dictionary<TaggregateId, ITaggregate> _heldTaggregates = new();
   readonly HashSet<TaggregateId> _deletedTaggregates = [];
   readonly List<IDisposable> _teventStreamSubscriptions = [];

   internal TeventStoreSession(TaggregateTeventStore store, TaggregateUnitOfWork unitOfWork)
   {
      _store = store;
      _unitOfWork = unitOfWork;
   }

   ///<summary>Whether this unit of work holds the taggregate <paramref name="id"/>, and if so its instance, or null when it is deleted.</summary>
   internal bool TryGetHeld(TaggregateId id, out ITaggregate? held)
   {
      var holds = _heldTaggregates.TryGetValue(id, out held);
      if(_deletedTaggregates.Contains(id)) held = null;
      return holds;
   }

   public bool TryGetForUpdate<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate
   {
      if(TryGetHeld(id, out var held))
      {
         taggregate = held == null ? null : TaggregateTeventStore.AsRequested<TTaggregate>(held);
         return taggregate != null;
      }

      //Read with the database's update lock, which the transaction holds until it completes.
      taggregate = _store.InstanceFrom<TTaggregate>(_store.TeventStore.GetTaggregateHistoryForUpdate(id));
      if(taggregate == null) return false;

      _heldTaggregates.Add(id, taggregate);
      FollowTheTeventStreamOf(taggregate);
      return true;
   }

   public void Add(ITaggregate taggregate)
   {
      var tevents = TakeTheUncommittedTeventsOf(taggregate);
      if(taggregate.Version > 0 && tevents.None() || tevents.Any() && tevents.Min(it => it.Tevent.TaggregateVersion) > 1)
      {
         throw new AttemptToAddAlreadyPersistedTaggregateException(taggregate);
      }

      if(taggregate.Version == 0 && tevents.None())
      {
         throw new AttemptToAddEmptyTaggregateException(taggregate);
      }

      //Held before its tevents are written and dispatched, so that a handler that gets it meanwhile gets this instance. A tevent a
      //handler publishes on it meanwhile stays uncommitted, and is written and dispatched after those, in the order published.
      _heldTaggregates.Add(taggregate.Id, taggregate);
      for(; tevents.Any(); tevents = TakeTheUncommittedTeventsOf(taggregate)) WriteAndDispatch(tevents);
      FollowTheTeventStreamOf(taggregate);
   }

   ///<summary>Writes and dispatches each tevent <paramref name="taggregate"/> publishes from now on, as it hands it out.</summary>
   void FollowTheTeventStreamOf(ITaggregate taggregate) =>
      _teventStreamSubscriptions.Add(taggregate.TeventStream.Subscribe(tevent => WriteAndDispatch([WithIdentity(tevent)])));

   void WriteAndDispatch(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> tevents)
   {
      _store.TeventStore.SaveSingleTaggregateTevents(tevents);
      foreach(var tevent in tevents)
      {
         if(tevent.Tevent is ITaggregateDeletedTevent) _deletedTaggregates.Add(tevent.Tevent.TaggregateId);
         _unitOfWork.Dispatch(tevent);
      }
   }

   static List<ITaggregateTevent<ITaggregateTeventWithIdentity>> TakeTheUncommittedTeventsOf(ITaggregate taggregate)
   {
      List<ITaggregateTevent<ITaggregateTeventWithIdentity>> taken = [];
      taggregate.Commit(committedTevents => taken = [..committedTevents.Select(WithIdentity)]);
      return taken;
   }

   ///<summary>A tevent a kept taggregate published, as the store keeps it: with the identity its type promises.</summary>
   ///<remarks>The store keeps only classes whose tevents have identity, so the cast can't fail while that holds.</remarks>
   static ITaggregateTevent<ITaggregateTeventWithIdentity> WithIdentity(ITaggregateTevent<ITaggregateTevent> publishedTevent) =>
      publishedTevent as ITaggregateTevent<ITaggregateTeventWithIdentity>
   ?? throw new InvalidOperationException($"{publishedTevent.Tevent.GetType().FullName} has no identity, so a tevent store can't keep it. A taggregate kept in a tevent store publishes tevents implementing {nameof(ITaggregateTeventWithIdentity)}.");

   public void AssertNoPublishHasFailed()
   {
      var failed = _heldTaggregates.Values.FirstOrDefault(it => ((ITaggregateStoreInternals)it).APublishHasFailed);
      State.Assert(failed == null, () => $"A publish failed on {failed!.GetType().Name} {failed.Id} in this unit of work, so its state can't be trusted. The unit of work rolls back.");
   }

   public void Prepare() {} //Each tevent was written as it was published, inside the transaction.

   public void Commit() {} //The database commits the writes with the transaction.

   public void Rollback() {} //The database rolls the writes back with the transaction.

   public void Complete() => _teventStreamSubscriptions.ForEach(it => it.Dispose());
}
