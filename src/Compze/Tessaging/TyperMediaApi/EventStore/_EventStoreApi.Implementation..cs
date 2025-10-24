using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Abstractions.MessageHandling.Registration;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Newtonsoft.Json;

namespace Compze.Tessaging.TyperMediaApi.EventStore;

public partial class EventStoreApi
{
   public partial class QueryApi
   {
      public class AggregateLink<TAggregate> : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<AggregateLink<TAggregate>, TAggregate> where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal AggregateLink(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (AggregateLink<TAggregate> query, IEventStoreUpdater updater) => updater.Get<TAggregate>(query.Id));
      }

      public class GetAggregateHistory<TEvent> : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<GetAggregateHistory<TEvent>, IEnumerable<TEvent>> where TEvent : IAggregateEvent
      {
         [JsonConstructor] internal GetAggregateHistory(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (GetAggregateHistory<TEvent> query, IEventStoreReader reader) => reader.GetHistory(query.Id).Cast<TEvent>());
      }

      public class GetReadonlyCopyOfAggregate<TAggregate> : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<GetReadonlyCopyOfAggregate<TAggregate>, TAggregate> where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal GetReadonlyCopyOfAggregate(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (GetReadonlyCopyOfAggregate<TAggregate> query, IEventStoreReader reader) => reader.GetReadonlyCopy<TAggregate>(query.Id));
      }

      public class GetReadonlyCopyOfAggregateVersion<TAggregate> : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<GetReadonlyCopyOfAggregateVersion<TAggregate>, TAggregate> where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal GetReadonlyCopyOfAggregateVersion(Guid id, int version)
         {
            Id = id;
            Version = version;
         }

         [JsonProperty] Guid Id { get; }
         [JsonProperty] int Version { get; }

         internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (GetReadonlyCopyOfAggregateVersion<TAggregate> query, IEventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TAggregate>(query.Id, query.Version));
      }
   }

   public partial class CommandApi
   {
      public class SaveAggregate<TAggregate> : MessageTypes.StrictlyLocal.Commands.StrictlyLocalCommand
         where TAggregate : class, IEventStored
      {
         [JsonConstructor] internal SaveAggregate(TAggregate entity) => Entity = entity;
         TAggregate Entity { get; }

         internal static void RegisterHandler(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
            (SaveAggregate<TAggregate> command, IEventStoreUpdater updater) => updater.Save(command.Entity));
      }
   }

   internal static void RegisterHandlersForAggregate<TAggregate, TEvent>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
      where TAggregate : class, IEventStored<TEvent>
      where TEvent : IAggregateEvent
   {
      CommandApi.SaveAggregate<TAggregate>.RegisterHandler(registrar);
      QueryApi.AggregateLink<TAggregate>.RegisterHandler(registrar);
      QueryApi.GetReadonlyCopyOfAggregate<TAggregate>.RegisterHandler(registrar);
      QueryApi.GetReadonlyCopyOfAggregateVersion<TAggregate>.RegisterHandler(registrar);
      QueryApi.GetAggregateHistory<TEvent>.RegisterHandler(registrar);
   }
}