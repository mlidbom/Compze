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
   readonly IAggregateTypeValidator _aggregateTypeValidator;
   readonly IDictionary<Guid, ITeventStored> _idMap = new Dictionary<Guid, ITeventStored>();
   readonly IUsageGuard _usageGuard;
   readonly List<IDisposable> _disposableResources = [];
   IUtcTimeTimeSource TimeSource { get; set; }

   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Scoped.For<ITeventStoreUpdater, ITeventStoreReader>()
               .CreatedBy((ITeventStoreTeventPublisher teventPublisher, ITeventStore teventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                             new TeventStoreUpdater(teventPublisher, teventStore, timeSource, aggregateTypeValidator)));

   TeventStoreUpdater(ITeventStoreTeventPublisher teventStoreTeventPublisher, ITeventStore store, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator)
   {
      Argument.NotNull(teventStoreTeventPublisher).NotNull(store).NotNull(timeSource);

      _usageGuard = new CombinationUsageGuard(new SingleThreadUseGuard(this), new SingleTransactionUsageGuard(this));
      _teventStoreTeventPublisher = teventStoreTeventPublisher;
      _store = store;
      _aggregateTypeValidator = aggregateTypeValidator;
      TimeSource = timeSource;
   }

   public TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : class, ITeventStored
   {
      _aggregateTypeValidator.AssertIsValid<TAggregate>();
      _usageGuard.EnsureAccessValid();
      if(!DoTryGet(aggregateId, out TAggregate? result))
      {
         throw new AggregateNotFoundException(aggregateId);
      }

      return result;
   }

   public bool TryGet<TAggregate>(Guid aggregateId, [MaybeNullWhen(false)] out TAggregate aggregate) where TAggregate : class, ITeventStored
   {
      _aggregateTypeValidator.AssertIsValid<TAggregate>();
      _usageGuard.EnsureAccessValid();
      return DoTryGet(aggregateId, out aggregate);
   }

   public TAggregate GetReadonlyCopy<TAggregate>(Guid aggregateId) where TAggregate : class, ITeventStored => LoadSpecificVersionInternal<TAggregate>(aggregateId, int.MaxValue, verifyVersion: false);

   public TAggregate GetReadonlyCopyOfVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : class, ITeventStored => LoadSpecificVersionInternal<TAggregate>(aggregateId, version);

   // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
   TAggregate LoadSpecificVersionInternal<TAggregate>(Guid aggregateId, int version, bool verifyVersion = true) where TAggregate : ITeventStored
   {
      _aggregateTypeValidator.AssertIsValid<TAggregate>();
      Argument.IsGreaterThan(version, 0);

      _usageGuard.EnsureAccessValid();

      var history = GetHistory(aggregateId);
      if(history.None())
      {
         throw new AggregateNotFoundException(aggregateId);
      }

      if(verifyVersion && history.Count < version - 1)
      {
         throw new Exception($"Requested version: {version} not found. Current version: {history.Count}");
      }

      var aggregate = CreateInstance<TAggregate>();
      aggregate.LoadFromHistory(history.Where(e => e.AggregateVersion <= version));
      return aggregate;
   }

   public void Save<TAggregate>(TAggregate aggregate) where TAggregate : class, ITeventStored
   {
      _aggregateTypeValidator.AssertIsValid<TAggregate>();
      _usageGuard.EnsureAccessValid();

      aggregate.Commit(tevents =>
      {
         if(aggregate.Version > 0 && tevents.None() || tevents.Any() && tevents.Min(e => e.AggregateVersion) > 1)
         {
            throw new AttemptToSaveAlreadyPersistedAggregateException(aggregate);
         }

         if(aggregate.Version == 0 && tevents.None())
         {
            throw new AttemptToSaveEmptyAggregateException(aggregate);
         }

         _store.SaveSingleAggregateTevents(tevents);

         tevents.ForEach(_teventStoreTeventPublisher.Publish);
      });

      _idMap.Add(aggregate.Id, aggregate);

      _disposableResources.Add(aggregate.TeventStream.Subscribe(OnAggregateTevent));
   }

   void OnAggregateTevent(IAggregateTevent tevent)
   {
      _usageGuard.EnsureAccessValid();
      if(!_idMap.ContainsKey(tevent.AggregateId))
      {
         throw new Exception($"Got tevent from aggregate that is not tracked! Id: {tevent.AggregateId}");
      }

      _store.SaveSingleAggregateTevents([tevent]);
      _teventStoreTeventPublisher.Publish(tevent);
   }

   public void Delete(Guid aggregateId)
   {
      _store.DeleteAggregate(aggregateId);
      _idMap.Remove(aggregateId);
   }

   public void Dispose()
   {
      _usageGuard.EnsureAccessValid();
      _disposableResources.ForEach(resource => resource.Dispose());
      _store.Dispose();
   }

   public override string ToString() => $"{_id}: {GetType().FullName}";
   readonly Guid _id = Guid.NewGuid();

   public IReadOnlyList<IAggregateTevent> GetHistory(Guid aggregateId) => GetHistoryInternal(aggregateId, takeWriteLock: false);

   IReadOnlyList<IAggregateTevent> GetHistoryInternal(Guid aggregateId, bool takeWriteLock) =>
      takeWriteLock
         ? _store.GetAggregateHistoryForUpdate(aggregateId)
         : _store.GetAggregateHistory(aggregateId);

   bool DoTryGet<TAggregate>(Guid aggregateId, [NotNullWhen(true)] out TAggregate? aggregate) where TAggregate : class, ITeventStored
   {
      if(_idMap.TryGetValue(aggregateId, out var teventStored))
      {
         aggregate = (TAggregate)teventStored;
         return true;
      }

      var history = GetHistoryInternal(aggregateId, takeWriteLock: true).ToList();
      if(history.Any())
      {
         aggregate = CreateInstance<TAggregate>();
         aggregate.LoadFromHistory(history);
         _idMap.Add(aggregateId, aggregate);
         _disposableResources.Add(aggregate.TeventStream.Subscribe(OnAggregateTevent));
         return true;
      } else
      {
         aggregate = null;
         return false;
      }
   }

   TAggregate CreateInstance<TAggregate>() where TAggregate : ITeventStored
   {
      var aggregate = Constructor.For<TAggregate>.DefaultConstructor.Instance();
      aggregate.SetTimeSource(TimeSource);
      return aggregate;
   }
}
