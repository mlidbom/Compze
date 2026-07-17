using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Teventive.Internal;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Public;
using Compze.Teventive.TeventStore.Abstractions.Public.Exceptions;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore;

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

      _usageGuard = new CombinationUsageGuard(new SingleThreadUseGuard(this), new SingleTransactionUsageGuard(this));
      _teventPublisher = teventPublisher;
      _store = store;
      _taggregateTypeValidator = taggregateTypeValidator;
   }

   public TTaggregate Get<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();
      if(!DoTryGet(taggregateId, out TTaggregate? result))
      {
         throw new TaggregateNotFoundException(taggregateId);
      }

      return result;
   }

   public bool TryGet<TTaggregate>(TaggregateId taggregateId, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();
      return DoTryGet(taggregateId, out taggregate);
   }

   public TTaggregate GetReadonlyCopy<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate => LoadSpecificVersionInternal<TTaggregate>(taggregateId, int.MaxValue, verifyVersion: false);

   public TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate => LoadSpecificVersionInternal<TTaggregate>(taggregateId, version);

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

      var taggregate = CreateInstance<TTaggregate>();
      taggregate.LoadFromHistory(history.Where(it => it.Tevent.TaggregateVersion <= version));
      return taggregate;
   }

   public void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();

      taggregate.Commit(wrappedTevents =>
      {
         if(taggregate.Version > 0 && wrappedTevents.None() || wrappedTevents.Any() && wrappedTevents.Min(it => it.Tevent.TaggregateVersion) > 1)
         {
            throw new AttemptToSaveAlreadyPersistedTaggregateException(taggregate);
         }

         if(taggregate.Version == 0 && wrappedTevents.None())
         {
            throw new AttemptToSaveEmptyTaggregateException(taggregate);
         }

         _store.SaveSingleTaggregateTevents(wrappedTevents);

         //The Teventive taggregate model raises tevents synchronously - from constructors and domain methods - and this session
         //forwards them where they are raised, inside the caller's unit of work. That synchronous context meets the exactly-once
         //publisher door's async-only contract here, in this one deliberate bridge; the alternative - an async taggregate domain
         //model - would be a redesign of Teventive itself, not a call-site choice.
         wrappedTevents.ForEach(wrappedTevent => _teventPublisher.PublishAsync(wrappedTevent).GetAwaiter().GetResult());
      });

      _idMap.Add(taggregate.Id, taggregate);

      _disposableResources.Add(taggregate.TeventStream.Subscribe(OnTaggregateTevent));
   }

   void OnTaggregateTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent)
   {
      _usageGuard.EnsureAccessValid();
      if(!_idMap.ContainsKey(wrappedTevent.Tevent.TaggregateId))
      {
         throw new Exception($"Got tevent from taggregate that is not tracked! Id: {wrappedTevent.Tevent.TaggregateId}");
      }

      _store.SaveSingleTaggregateTevents([wrappedTevent]);
      //The same deliberate sync-context bridge as in Save: the taggregate's tevent stream raises synchronously mid-domain-call.
      _teventPublisher.PublishAsync(wrappedTevent).GetAwaiter().GetResult();
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

   public IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetHistory(TaggregateId taggregateId) => GetHistoryInternal(taggregateId, takeWriteLock: false);

   IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetHistoryInternal(TaggregateId taggregateId, bool takeWriteLock) =>
      takeWriteLock
         ? _store.GetTaggregateHistoryForUpdate(taggregateId)
         : _store.GetTaggregateHistory(taggregateId);

   bool DoTryGet<TTaggregate>(TaggregateId taggregateId, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate
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

   static TTaggregate CreateInstance<TTaggregate>() where TTaggregate : ITaggregate
   {
      var taggregate = Constructor.For<TTaggregate>.DefaultConstructor.Instance();
      return taggregate;
   }
}
