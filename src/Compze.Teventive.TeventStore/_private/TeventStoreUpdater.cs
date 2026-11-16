using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Tessaging.TessageBus;
using Compze.Teventive._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore._private;

class TeventStoreUpdater : ITeventStoreReader, ITeventStoreUpdater
{
   readonly IUnitOfWorkTeventPublisher _teventPublisher;
   readonly ITeventStore _store;
   readonly ITaggregateTypeValidator _taggregateTypeValidator;
   readonly IDictionary<TaggregateId, ITaggregate> _idMap = new Dictionary<TaggregateId, ITaggregate>();
   readonly IUsageGuard _usageGuard;
   readonly List<IDisposable> _disposableResources = [];

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Scoped.For<ITeventStoreUpdater, ITeventStoreReader>()
               .CreatedBy((IUnitOfWorkTeventPublisher teventPublisher, ITeventStore teventStore, ITaggregateTypeValidator taggregateTypeValidator) =>
                             new TeventStoreUpdater(teventPublisher, teventStore, taggregateTypeValidator)));

   TeventStoreUpdater(IUnitOfWorkTeventPublisher teventPublisher, ITeventStore store, ITaggregateTypeValidator taggregateTypeValidator)
   {
      Argument.NotNull(teventPublisher).NotNull(store);

      //Transaction affinity, never thread affinity: an async unit of work legitimately migrates across threads, and the
      //misuse that must fail loud is one session serving two transactions.
      _usageGuard = new SingleTransactionUsageGuard(this);
      _teventPublisher = teventPublisher;
      _store = store;
      _taggregateTypeValidator = taggregateTypeValidator;
   }

   public TTaggregate Get<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();
      if(!DoTryGet(taggregateId, out TTaggregate? result))
      {
         throw new TaggregateNotFoundException(taggregateId);
      }

      return result;
   }

   public bool TryGet<TTaggregate>(TaggregateId taggregateId, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();
      return DoTryGet(taggregateId, out taggregate);
   }

   public TTaggregate GetReadonlyCopy<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity> => LoadSpecificVersionInternal<TTaggregate>(taggregateId, int.MaxValue, verifyVersion: false);

   public TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity> => LoadSpecificVersionInternal<TTaggregate>(taggregateId, version);

   // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
   TTaggregate LoadSpecificVersionInternal<TTaggregate>(TaggregateId taggregateId, int version, bool verifyVersion = true) where TTaggregate : ITaggregate
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      Argument.Assert(version > 0);

      _usageGuard.EnsureAccessValid();

      var history = GetHistory(taggregateId);
      if(history.None())
      {
         throw new TaggregateNotFoundException(taggregateId);
      }

      if(verifyVersion && history.Count < version - 1)
      {
         throw new Exception($"Requested version: {version} not found. Current version: {history.Count}");
      }

      //urgent: The "readonly copy" this returns is only untracked: publishing on it changes it, and nothing says that the
      //   change isn't saved. Make it read only, so that publishing on it throws, as the committed instances of the store
      //   that keeps taggregates as their state do (dev_docs/taggregate-improvements/shared/the-taggregate-store.md)
      //   (Magnus, 2026-11-16).
      var taggregate = CreateInstance<TTaggregate>();
      taggregate.LoadFromHistory(history.Where(it => it.Tevent.TaggregateVersion <= version));
      return taggregate;
   }

   public void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();

      var wrappedTevents = TakeTheUncommittedTeventsOf(taggregate);
      if(taggregate.Version > 0 && wrappedTevents.None() || wrappedTevents.Any() && wrappedTevents.Min(it => it.Tevent.TaggregateVersion) > 1)
      {
         throw new AttemptToSaveAlreadyPersistedTaggregateException(taggregate);
      }

      if(taggregate.Version == 0 && wrappedTevents.None())
      {
         throw new AttemptToSaveEmptyTaggregateException(taggregate);
      }

      //Tracked before its tevents are published, so that a handler that gets it meanwhile gets this instance. A tevent a handler
      //publishes on it meanwhile stays uncommitted, and is saved and published after those, in the order published.
      _idMap.Add(taggregate.Id, taggregate);
      for(; wrappedTevents.Any(); wrappedTevents = TakeTheUncommittedTeventsOf(taggregate)) SaveAndPublish(wrappedTevents);

      _disposableResources.Add(taggregate.TeventStream.Subscribe(OnTaggregateTevent));
   }

   static List<ITaggregateTevent<ITaggregateTeventWithIdentity>> TakeTheUncommittedTeventsOf(ITaggregate taggregate)
   {
      List<ITaggregateTevent<ITaggregateTeventWithIdentity>> taken = [];
      taggregate.Commit(committedTevents => taken = [.. committedTevents.Select(WithIdentity)]);
      return taken;
   }

   void SaveAndPublish(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> wrappedTevents)
   {
      _store.SaveSingleTaggregateTevents(wrappedTevents);

      //The Teventive taggregate model raises tevents synchronously - from constructors and domain methods - and this session
      //forwards them where they are raised, inside the caller's unit of work. That synchronous context meets the exactly-once
      //publisher's async-only contract here, in this one deliberate bridge; the alternative - an async taggregate domain
      //model - would be a redesign of Teventive itself, not a call-site choice.
      foreach(var wrappedTevent in wrappedTevents) _teventPublisher.PublishAsync(wrappedTevent).GetAwaiter().GetResult();
   }

   void OnTaggregateTevent(ITaggregateTevent<ITaggregateTevent> publishedTevent)
   {
      var wrappedTevent = WithIdentity(publishedTevent);
      _usageGuard.EnsureAccessValid();
      if(!_idMap.ContainsKey(wrappedTevent.Tevent.TaggregateId))
      {
         throw new Exception($"Got tevent from taggregate that is not tracked! Id: {wrappedTevent.Tevent.TaggregateId}");
      }

      SaveAndPublish([wrappedTevent]);
   }

   public void Delete(TaggregateId taggregateId)
   {
      _store.DeleteTaggregate(taggregateId);
      _idMap.Remove(taggregateId);
   }

   public void Dispose()
   {
      _usageGuard.EnsureAccessValid();
      _disposableResources.ForEach(resource => resource.Dispose());
      _store.Dispose();
   }

   public IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetHistory(TaggregateId taggregateId) => GetHistoryInternal(taggregateId, takeWriteLock: false);

   IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetHistoryInternal(TaggregateId taggregateId, bool takeWriteLock) =>
      takeWriteLock
         ? _store.GetTaggregateHistoryForUpdate(taggregateId)
         : _store.GetTaggregateHistory(taggregateId);

   bool DoTryGet<TTaggregate>(TaggregateId taggregateId, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
   {
      if(_idMap.TryGetValue(taggregateId, out var teventStored))
      {
         taggregate = (TTaggregate)teventStored;
         return true;
      }

      var history = GetHistoryInternal(taggregateId, takeWriteLock: true).ToList();
      if(history.Any())
      {
         taggregate = CreateInstance<TTaggregate>();
         taggregate.LoadFromHistory(history);
         _idMap.Add(taggregateId, taggregate);
         _disposableResources.Add(taggregate.TeventStream.Subscribe(OnTaggregateTevent));
         return true;
      } else
      {
         taggregate = null;
         return false;
      }
   }

   ///<summary>A tevent a kept taggregate published, as the store keeps it: with the identity its type promises.</summary>
   ///<remarks>The store's methods accept only taggregates whose tevents have identity, so the cast can't fail while they hold.</remarks>
   static ITaggregateTevent<ITaggregateTeventWithIdentity> WithIdentity(ITaggregateTevent<ITaggregateTevent> publishedTevent) =>
      publishedTevent as ITaggregateTevent<ITaggregateTeventWithIdentity>
   ?? throw new InvalidOperationException($"{publishedTevent.Tevent.GetType().FullName} has no identity, so a tevent store can't keep it. A taggregate kept in a tevent store publishes tevents implementing {nameof(ITaggregateTeventWithIdentity)}.");

   static TTaggregate CreateInstance<TTaggregate>() where TTaggregate : ITaggregate
   {
      var taggregate = Constructor.For<TTaggregate>.DefaultConstructor.Instance();
      return taggregate;
   }
}
