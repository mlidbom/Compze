using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Newtonsoft.Json;

namespace Compze.Tessaging.TyperMediaApi.EventStore;

public partial class TeventStoreApi
{
   public partial class TueryApi
   {
      public class TaggregateLink<TTaggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<TaggregateLink<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         [JsonConstructor] internal TaggregateLink(Guid id) => Id = id;
         [JsonProperty] Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (TaggregateLink<TTaggregate> tuery, ITeventStoreUpdater updater) => updater.Get<TTaggregate>(tuery.Id));
      }

      public class GetTaggregateHistory<TTevent> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetTaggregateHistory<TTevent>, IEnumerable<TTevent>> where TTevent : ITaggregateTevent
      {
         internal GetTaggregateHistory(Guid id) => Id = id;
         Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetTaggregateHistory<TTevent> tuery, ITeventStoreReader reader) => reader.GetHistory(tuery.Id).Cast<TTevent>());
      }

      public class GetReadonlyCopyOfTaggregate<TTaggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetReadonlyCopyOfTaggregate<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         internal GetReadonlyCopyOfTaggregate(Guid id) => Id = id;
         Guid Id { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetReadonlyCopyOfTaggregate<TTaggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopy<TTaggregate>(tuery.Id));
      }

      public class GetReadonlyCopyOfTaggregateVersion<TTaggregate> : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         internal GetReadonlyCopyOfTaggregateVersion(Guid id, int version)
         {
            Id = id;
            Version = version;
         }

         Guid Id { get; }
         int Version { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetReadonlyCopyOfTaggregateVersion<TTaggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TTaggregate>(tuery.Id, tuery.Version));
      }
   }

   public partial class TommandApi
   {
      public class SaveTaggregate<TTaggregate> : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand
         where TTaggregate : class, ITaggregate
      {
         internal SaveTaggregate(TTaggregate entity) => Entity = entity;
         TTaggregate Entity { get; }

         internal static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
            (SaveTaggregate<TTaggregate> tommand, ITeventStoreUpdater updater) => updater.Save(tommand.Entity));
      }
   }

   internal static void RegisterHandlersForTaggregate<TTaggregate, TTevent>(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      TommandApi.SaveTaggregate<TTaggregate>.RegisterHandler(registrar);
      TueryApi.TaggregateLink<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetTaggregateHistory<TTevent>.RegisterHandler(registrar);
   }
}