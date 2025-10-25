using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Hosting.MessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Newtonsoft.Json;

namespace Compze.Tessaging.TyperMediaApi.EventStore;

public partial class EventStoreApi
{
   public partial class QueryApi
   {
      public class AggregateLink<TAggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<AggregateLink<TAggregate>, TAggregate> where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal AggregateLink(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (AggregateLink<TAggregate> query, IEventStoreUpdater updater) => updater.Get<TAggregate>(query.Id));
      }

      public class GetAggregateHistory<TEvent> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetAggregateHistory<TEvent>, IEnumerable<TEvent>> where TEvent : IAggregateTevent
      {
         [JsonConstructor] internal GetAggregateHistory(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (GetAggregateHistory<TEvent> query, IEventStoreReader reader) => reader.GetHistory(query.Id).Cast<TEvent>());
      }

      public class GetReadonlyCopyOfAggregate<TAggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetReadonlyCopyOfAggregate<TAggregate>, TAggregate> where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal GetReadonlyCopyOfAggregate(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (GetReadonlyCopyOfAggregate<TAggregate> query, IEventStoreReader reader) => reader.GetReadonlyCopy<TAggregate>(query.Id));
      }

      public class GetReadonlyCopyOfAggregateVersion<TAggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetReadonlyCopyOfAggregateVersion<TAggregate>, TAggregate> where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal GetReadonlyCopyOfAggregateVersion(Guid id, int version)
         {
            Id = id;
            Version = version;
         }

         [JsonProperty] Guid Id { get; }
         [JsonProperty] int Version { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (GetReadonlyCopyOfAggregateVersion<TAggregate> query, IEventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TAggregate>(query.Id, query.Version));
      }
   }

   public partial class CommandApi
   {
      public class SaveAggregate<TAggregate> : TessageTypes.StrictlyLocal.Commands.StrictlyLocalTommand
         where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal SaveAggregate(TAggregate entity) => Entity = entity;
         TAggregate Entity { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
            (SaveAggregate<TAggregate> command, IEventStoreUpdater updater) => updater.Save(command.Entity));
      }
   }

   internal static void RegisterHandlersForAggregate<TAggregate, TEvent>(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
      where TAggregate : class, IEventStored<TEvent>
      where TEvent : IAggregateTevent
   {
      CommandApi.SaveAggregate<TAggregate>.RegisterHandler(registrar);
      QueryApi.AggregateLink<TAggregate>.RegisterHandler(registrar);
      QueryApi.GetReadonlyCopyOfAggregate<TAggregate>.RegisterHandler(registrar);
      QueryApi.GetReadonlyCopyOfAggregateVersion<TAggregate>.RegisterHandler(registrar);
      QueryApi.GetAggregateHistory<TEvent>.RegisterHandler(registrar);
   }
}