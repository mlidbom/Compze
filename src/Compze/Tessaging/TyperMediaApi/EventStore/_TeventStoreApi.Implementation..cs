using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Newtonsoft.Json;

namespace Compze.Tessaging.TyperMediaApi.TeventStore;

public partial class TeventStoreApi
{
   public partial class TueryApi
   {
      public class AggregateLink<TAggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<AggregateLink<TAggregate>, TAggregate> where TAggregate : class, ITeventStored
      {
         [JsonConstructor] internal AggregateLink(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (AggregateLink<TAggregate> tuery, ITeventStoreUpdater updater) => updater.Get<TAggregate>(tuery.Id));
      }

      public class GetAggregateHistory<TTevent> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetAggregateHistory<TTevent>, IEnumerable<TTevent>> where TTevent : IAggregateTevent
      {
         [JsonConstructor] internal GetAggregateHistory(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetAggregateHistory<TTevent> tuery, ITeventStoreReader reader) => reader.GetHistory(tuery.Id).Cast<TTevent>());
      }

      public class GetReadonlyCopyOfAggregate<TAggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetReadonlyCopyOfAggregate<TAggregate>, TAggregate> where TAggregate : class, ITeventStored
      {
         [JsonConstructor] internal GetReadonlyCopyOfAggregate(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetReadonlyCopyOfAggregate<TAggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopy<TAggregate>(tuery.Id));
      }

      public class GetReadonlyCopyOfAggregateVersion<TAggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetReadonlyCopyOfAggregateVersion<TAggregate>, TAggregate> where TAggregate : class, ITeventStored
      {
         [JsonConstructor] internal GetReadonlyCopyOfAggregateVersion(Guid id, int version)
         {
            Id = id;
            Version = version;
         }

         [JsonProperty] Guid Id { get; }
         [JsonProperty] int Version { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetReadonlyCopyOfAggregateVersion<TAggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TAggregate>(tuery.Id, tuery.Version));
      }
   }

   public partial class TommandApi
   {
      public class SaveAggregate<TAggregate> : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand
         where TAggregate : class, ITeventStored
      {
         [JsonConstructor] internal SaveAggregate(TAggregate entity) => Entity = entity;
         TAggregate Entity { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
            (SaveAggregate<TAggregate> tommand, ITeventStoreUpdater updater) => updater.Save(tommand.Entity));
      }
   }

   internal static void RegisterHandlersForAggregate<TAggregate, TTevent>(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
      where TAggregate : class, ITeventStored<TTevent>
      where TTevent : IAggregateTevent
   {
      TommandApi.SaveAggregate<TAggregate>.RegisterHandler(registrar);
      TueryApi.AggregateLink<TAggregate>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfAggregate<TAggregate>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfAggregateVersion<TAggregate>.RegisterHandler(registrar);
      TueryApi.GetAggregateHistory<TTevent>.RegisterHandler(registrar);
   }
}