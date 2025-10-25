using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReactiveCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Internal;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public.Exceptions;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time.Public;
using Compze.Utilities.Threading;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Teventive.TeventStore;

class TeventStoreUpdater : ITeventStoreReader, ITeventStoreUpdater
{
   readonly ITeventStoreTeventPublisher _teventStoreTeventPublisher;
   readonly ITeventStore _store;
   readonly ITaggregateTypeValidator _taggregateTypeValidator;
   readonly IDictionary<Guid, ITeventStored> _idMap = new Dictionary<Guid, ITeventStored>();
   readonly IUsageGuard _usageGuard;
   readonly List<IDisposable> _disposableResources = [];
   IUtcTimeTimeSource TimeSource { get; set; }

   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Scoped.For<ITeventStoreUpdater, ITeventStoreReader>()
               .CreatedBy((ITeventStoreTeventPublisher teventPublisher, ITeventStore teventStore, IUtcTimeTimeSource timeSource, ITaggregateTypeValidator taggregateTypeValidator) =>
                             new TeventStoreUpdater(teventPublisher, teventStore, timeSource, taggregateTypeValidator)));

   TeventStoreUpdater(ITeventStoreTeventPublisher teventStoreTeventPublisher, ITeventStore store, IUtcTimeTimeSource timeSource, ITaggregateTypeValidator taggregateTypeValidator)
   {
      Argument.NotNull(teventStoreTeventPublisher).NotNull(store).NotNull(timeSource);

      _usageGuard = new CombinationUsageGuard(new SingleThreadUseGuard(this), new SingleTransactionUsageGuard(this));
      _teventStoreTeventPublisher = teventStoreTeventPublisher;
      _store = store;
      _taggregateTypeValidator = taggregateTypeValidator;
      TimeSource = timeSource;
   }

   public TTaggregate Get<TTaggregate>(Guid taggregateId) where TTaggregate : class, ITeventStored
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();
      if(!DoTryGet(taggregateId, out TTaggregate? result))
      {
         throw new TaggregateNotFoundException(taggregateId);
      }

      return result;
   }

   public bool TryGet<TTaggregate>(Guid taggregateId, [MaybeNullWhen(false)] out TTaggregate taggregate) where TTaggregate : class, ITeventStored
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();
      return DoTryGet(taggregateId, out taggregate);
   }

   public TTaggregate GetReadonlyCopy<TTaggregate>(Guid taggregateId) where TTaggregate : class, ITeventStored => LoadSpecificVersionInternal<TTaggregate>(taggregateId, int.MaxValue, verifyVersion: false);

   public TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(Guid taggregateId, int version) where TTaggregate : class, ITeventStored => LoadSpecificVersionInternal<TTaggregate>(taggregateId, version);

   // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
   TTaggregate LoadSpecificVersionInternal<TTaggregate>(Guid taggregateId, int version, bool verifyVersion = true) where TTaggregate : ITeventStored
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      Argument.IsGreaterThan(version, 0);

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
      taggregate.LoadFromHistory(history.Where(e => e.TaggregateVersion <= version));
      return taggregate;
   }

   public void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITeventStored
   {
      _taggregateTypeValidator.AssertIsValid<TTaggregate>();
      _usageGuard.EnsureAccessValid();

      taggregate.Commit(tevents =>
      {
         if(taggregate.Version > 0 && tevents.None() || tevents.Any() && tevents.Min(e => e.TaggregateVersion) > 1)
         {
            throw new AttemptToSaveAlreadyPersistedAggregateException(taggregate);
         }

         if(taggregate.Version == 0 && tevents.None())
         {
            throw new AttemptToSaveEmptyAggregateException(taggregate);
         }

         _store.SaveSingleTaggregateTevents(tevents);

         tevents.ForEach(_teventStoreTeventPublisher.Publish);
      });

      _idMap.Add(taggregate.Id, taggregate);

      _disposableResources.Add(taggregate.TeventStream.Subscribe(OnTaggregateTevent));
   }

   void OnTaggregateTevent(ITaggregateTevent tevent)
   {
      _usageGuard.EnsureAccessValid();
      if(!_idMap.ContainsKey(tevent.TaggregateId))
      {
         throw new Exception($"Got tevent from taggregate that is not tracked! Id: {tevent.TaggregateId}");
      }

      _store.SaveSingleTaggregateTevents([tevent]);
      _teventStoreTeventPublisher.Publish(tevent);
   }

   public void Delete(Guid taggregateId)
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

   public override string ToString() => $"{_id}: {GetType().FullName}";
   readonly Guid _id = Guid.NewGuid();

   public IReadOnlyList<ITaggregateTevent> GetHistory(Guid taggregateId) => GetHistoryInternal(taggregateId, takeWriteLock: false);

   IReadOnlyList<ITaggregateTevent> GetHistoryInternal(Guid taggregateId, bool takeWriteLock) =>
      takeWriteLock
         ? _store.GetTaggregateHistoryForUpdate(taggregateId)
         : _store.GetTaggregateHistory(taggregateId);

   bool DoTryGet<TTaggregate>(Guid taggregateId, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITeventStored
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

   TTaggregate CreateInstance<TTaggregate>() where TTaggregate : ITeventStored
   {
      var taggregate = Constructor.For<TTaggregate>.DefaultConstructor.Instance();
      taggregate.SetTimeSource(TimeSource);
      return taggregate;
   }
}
