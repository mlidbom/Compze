using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Internals.SystemCE.ReflectionCE;
using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Teventive.TeventStore.Public;
using Compze.Tessaging.Teventive.TeventStore.Public.Exceptions;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Contracts;
using Compze.Teventive;
using Compze.Teventive.Internal;
using Compze.Teventive.Taggregates.Tevents.Public;
using static Compze.Contracts.Contract;

namespace Compze.Tessaging.Teventive.TeventStore;

class TeventStoreUpdater : ITeventStoreReader, ITeventStoreUpdater
{
   readonly ITeventPublisher _teventPublisher;
   readonly ITeventStore _store;
   readonly ITaggregateTypeValidator _taggregateTypeValidator;
   readonly IDictionary<TaggregateId, ITaggregate> _idMap = new Dictionary<TaggregateId, ITaggregate>();
   readonly IUsageGuard _usageGuard;
   readonly List<IDisposable> _disposableResources = [];

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Scoped.For<ITeventStoreUpdater, ITeventStoreReader>()
               .CreatedBy((ITeventPublisher teventPublisher, ITeventStore teventStore, ITaggregateTypeValidator taggregateTypeValidator) =>
                             new TeventStoreUpdater(teventPublisher, teventStore, taggregateTypeValidator)));

   TeventStoreUpdater(ITeventPublisher teventPublisher, ITeventStore store, ITaggregateTypeValidator taggregateTypeValidator)
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

         wrappedTevents.ForEach(wrappedTevent => _teventPublisher.Publish(wrappedTevent));
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
      _teventPublisher.Publish(wrappedTevent);
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
